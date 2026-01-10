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
    private readonly IEventPublisher _events; // <-- changed from ITenantEventPublisher
    private readonly IHostEnvironment _env;
    private readonly ProvisioningOptions _opts;
    private readonly KafkaOptions _kafka;     // <-- to read topic name

    // Keep if you truly need optional service provisioning (you can remove later)
    private static readonly string[] TenantServices =
    [
        "billing",
        "stationmgmt",
        "provider",
        "tracking",
        "reviews"
    ];

    public ProviderProvisioningService(
        IProviderRepository providers,
        IKeycloakProvisioningClient keycloak,
        IEventPublisher events,
        IHostEnvironment env,
        IOptions<ProvisioningOptions> opts,
        IOptions<KafkaOptions> kafkaOpts)
    {
        _providers = providers;
        _keycloak = keycloak;
        _events = events;
        _env = env;
        _opts = opts.Value;
        _kafka = kafkaOpts.Value;
    }

    public async Task<RegisterProviderResult> RegisterProviderAsync(RegisterProviderRequest request, CancellationToken ct)
    {
        var providerId = (request.ProviderId ?? "").Trim().ToLowerInvariant();
        var displayName = (request.DisplayName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("providerId is required");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("displayName is required");

        if (await _providers.ExistsAsync(providerId, ct))
            throw new InvalidOperationException($"Provider '{providerId}' already exists.");

        var provider = new Provider(providerId, displayName);
        await _providers.CreateAsync(provider, ct);

        // Auto-generate admin user details
        var adminUsername = providerId + _opts.ProviderAdminUsernameSuffix;
        var adminEmail = $"{adminUsername}@{_opts.ProviderAdminEmailDomain}";
        var tempPassword = GenerateTempPassword(_opts.TemporaryPasswordLength);

        // Create/Update Keycloak (real)
        await _keycloak.EnsureProviderAdminUserAsync(
            providerId: providerId,
            displayName: displayName,
            adminUsername: adminUsername,
            adminEmail: adminEmail,
            temporaryPassword: tempPassword,
            ct: ct);

        // Payload only (no envelope fields here)
        var payload = new ProviderProvisioned
        {
            ProviderId = providerId,
            DisplayName = displayName,
            Environment = _env.EnvironmentName.ToLowerInvariant(),
            TenantServices = TenantServices.ToList(),
            KeycloakAdminUsername = adminUsername,
            KeycloakAdminEmail = adminEmail
        };

        // Publish using envelope pattern (partner-aligned)
        var topic = string.IsNullOrWhiteSpace(_kafka.TenantEventsTopic)
            ? "tenant.events"
            : _kafka.TenantEventsTopic;

        await _events.PublishAsync(
            topic: topic,
            eventType: "ProviderProvisioned",
            key: providerId,
            payload: payload,
            ct: ct);

        return new RegisterProviderResult(providerId, displayName, adminUsername, adminEmail, tempPassword);
    }

    private static string GenerateTempPassword(int length)
    {
        // URL-safe-ish base64, trimmed to length; good enough for MVP.
        var bytes = RandomNumberGenerator.GetBytes(Math.Max(24, length));
        var b64 = Convert.ToBase64String(bytes)
            .Replace('+', 'A')
            .Replace('/', 'B')
            .Replace("=", "");

        var s = b64.Length >= length ? b64[..length] : (b64 + b64)[..length];

        // Ensure variety
        if (!s.Any(char.IsUpper)) s = "A" + s[1..];
        if (!s.Any(char.IsLower)) s = s[..^1] + "a";
        if (!s.Any(char.IsDigit)) s = s[..^2] + "1" + s[^1];
        if (!s.Contains('!')) s = s[..^1] + "!";

        return s;
    }
}
