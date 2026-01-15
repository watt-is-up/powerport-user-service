using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UserService.Application.Abstractions;
using UserService.Domain.Providers;

namespace UserService.Application.Providers.RegisterProvider;

public sealed class ProviderProvisioningService : IProviderProvisioningService
{
    private readonly IProviderRepository _providers;
    private readonly IKeycloakProvisioningClient _keycloak;
    private readonly ITenantInfraProvisioner _infra;
    private readonly IEventPublisher _events;
    private readonly IHostEnvironment _env;
    private readonly ProvisioningOptions _opts;
    private readonly KafkaOptions _kafka;

    public ProviderProvisioningService(
        IProviderRepository providers,
        IKeycloakProvisioningClient keycloak,
        ITenantInfraProvisioner infra,
        IEventPublisher events,
        IHostEnvironment env,
        IOptions<ProvisioningOptions> opts,
        IOptions<KafkaOptions> kafkaOpts)
    {
        _providers = providers;
        _keycloak = keycloak;
        _infra = infra;
        _events = events;
        _env = env;
        _opts = opts.Value;
        _kafka = kafkaOpts.Value;
    }

    public async Task<RegisterProviderResult> RegisterProviderAsync(RegisterProviderRequest request, CancellationToken ct)
    {
        var providerId = request.ProviderId;
        var displayName = (request.DisplayName ?? "").Trim();
        var uniqueName = (request.UniqueName ?? "").Trim();
        var providerEmail = (request.ProviderEmail ?? "").Trim();

        if (string.IsNullOrWhiteSpace(uniqueName)) throw new ArgumentException("uniqueName is required");
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("displayName is required");
        if (string.IsNullOrWhiteSpace(providerEmail)) throw new ArgumentException("providerEmail is required");

        if (await _providers.ExistsAsync(uniqueName, ct))
            throw new InvalidOperationException($"Provider '{uniqueName}' already exists.");

        var tenantId = providerId ?? Guid.NewGuid();

        // Store provider (with email + tenant)
        var provider = new Provider(uniqueName, displayName, providerEmail, tenantId);
        await _providers.CreateAsync(provider, ct);

        var envName = _env.EnvironmentName.ToLowerInvariant();

        // ============== IMPORTANT ==============
        // var infra = await _infra.EnsureTenantDatabasesAsync(uniqueName, envName, ct);
        // =======================================

        // Provider admin user
        var adminUsername = uniqueName + _opts.ProviderAdminUsernameSuffix;

        // IMPORTANT: use the admin-provided email
        var adminEmail = providerEmail;

        var tempPassword = GenerateTempPassword(_opts.TemporaryPasswordLength);

        await _keycloak.EnsureProviderAdminUserAsync(
            uniqueName: uniqueName,
            displayName: displayName,
            adminUsername: adminUsername,
            adminEmail: adminEmail,
            tenantId: tenantId.ToString(),
            temporaryPassword: tempPassword,
            ct: ct);

        // 3) Publish event: "Tenant created, DBs ready, run migrations"
        var payload = new ProviderProvisionedV2
        {
            TenantId = tenantId,
            ProviderUniqueName = uniqueName,
            DisplayName = displayName,
            Environment = envName,
            // ConnectionSecretNames = infra.ConnectionSecretNames,
            // DatabaseNames = infra.DatabaseNames
            ConnectionSecretNames = new Dictionary<string, string> { { "service-placeholder", "secret-placeholder" } },
            DatabaseNames = new Dictionary<string, string> { { "service-placeholder", "db-placeholder" } }
        };

        // Publish using envelope pattern (partner-aligned)
        var topic = string.IsNullOrWhiteSpace(_kafka.UserServiceTopic)
            ? "user.events"
            : _kafka.UserServiceTopic;

        // [TODO] - Publish TenantId along with the newly created User
        await _events.PublishAsync(
            topic: topic,
            eventType: "ProviderProvisionedV2",
            key: uniqueName,
            payload: payload,
            ct: ct);

        // Return temp password only in HTTP response (not Kafka)
        return new RegisterProviderResult(uniqueName, tenantId, displayName, adminUsername, adminEmail, tempPassword);
    }

    private static string GenerateTempPassword(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(Math.Max(24, length));
        var b64 = Convert.ToBase64String(bytes).Replace('+', 'A').Replace('/', 'B').Replace("=", "");
        var s = b64.Length >= length ? b64[..length] : (b64 + b64)[..length];
        if (!s.Any(char.IsUpper)) s = "A" + s[1..];
        if (!s.Any(char.IsLower)) s = s[..^1] + "a";
        if (!s.Any(char.IsDigit)) s = s[..^2] + "1" + s[^1];
        if (!s.Contains('!')) s = s[..^1] + "!";
        return s;
    }
}