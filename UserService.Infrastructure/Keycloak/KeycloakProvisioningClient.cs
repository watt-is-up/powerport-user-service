using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Application.Abstractions;

namespace UserService.Infrastructure.Keycloak;

public sealed class KeycloakProvisioningClient : IKeycloakProvisioningClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly KeycloakOptions _opts;
    private readonly ILogger<KeycloakProvisioningClient> _logger;

    public KeycloakProvisioningClient(
        HttpClient http,
        IOptions<KeycloakOptions> opts,
        ILogger<KeycloakProvisioningClient> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task EnsureProviderAdminUserAsync(
        string providerId,
        string displayName,
        string adminUsername,
        string adminEmail,
        string temporaryPassword,
        CancellationToken ct)
    {
        var token = await GetAdminAccessTokenAsync(ct);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var userId = await FindUserIdByUsernameAsync(_opts.Realm, adminUsername, ct);
        if (userId is null)
        {
            userId = await CreateUserAsync(_opts.Realm, providerId, adminUsername, adminEmail, ct);
            _logger.LogInformation("Keycloak: created user {Username} id={UserId}", adminUsername, userId);
        }
        else
        {
            await UpdateUserAsync(_opts.Realm, userId, providerId, adminUsername, adminEmail, ct);
            _logger.LogInformation("Keycloak: updated user {Username} id={UserId}", adminUsername, userId);
        }

        await SetPasswordAsync(_opts.Realm, userId, temporaryPassword, temporary: true, ct);

        if (_opts.ForcePasswordUpdateOnFirstLogin)
            await SetRequiredActionsAsync(_opts.Realm, userId, new[] { "UPDATE_PASSWORD" }, ct);

        await EnsureRealmRoleAssignedAsync(_opts.Realm, userId, _opts.UserRole, ct);
    }
    
    // -------- token --------
    private async Task<string> GetAdminAccessTokenAsync(CancellationToken ct)
    {
        var tokenUrl = $"{_opts.BaseUrl}/realms/{_opts.AdminRealm}/protocol/openid-connect/token";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _opts.AdminClientId,
            ["username"] = _opts.AdminUsername,
            ["password"] = _opts.AdminPassword
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak admin token failed ({(int)resp.StatusCode}): {body}");

        var tr = JsonSerializer.Deserialize<TokenResponse>(body, JsonOpts)
                 ?? throw new InvalidOperationException("Keycloak token parse failed");

        if (string.IsNullOrWhiteSpace(tr.AccessToken))
            throw new InvalidOperationException("Keycloak token missing access_token");

        return tr.AccessToken!;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    // -------- users --------
    private async Task<string?> FindUserIdByUsernameAsync(string realm, string username, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users?username={Uri.EscapeDataString(username)}&exact=true";
        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak users query failed ({(int)resp.StatusCode}): {body}");

        var users = JsonSerializer.Deserialize<List<UserRep>>(body, JsonOpts) ?? new();
        return users.FirstOrDefault()?.Id;
    }

    private string ResolveTenantId(string? providerId)
    {
        return !string.IsNullOrWhiteSpace(providerId)
            ? providerId
            : _opts.DefaultTenantId;
    }


    private async Task<string> CreateUserAsync(string realm, string? providerId, string username, string email, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users";

        var tenantId = ResolveTenantId(providerId);
        var role = providerId is not null ? "Provider" : "User";

        var attributes = new Dictionary<string, string[]>
        {
            [_opts.TenantIdAttributeName] = new[] { tenantId },
            [_opts.UserRole] = new[] { role }
        };

        if (!string.IsNullOrWhiteSpace(providerId))
        {
            attributes[_opts.ProviderIdAttributeName] = new[] { providerId };
        }

        var rep = new UserRep
        {
            Username = username,
            Enabled = true,
            Email = email,
            EmailVerified = _opts.RequireEmailVerified,
            Attributes = attributes,
        };

        using var resp = await _http.PostAsJsonAsync(url, rep, JsonOpts, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (resp.StatusCode != System.Net.HttpStatusCode.Created)
            throw new InvalidOperationException($"Keycloak create user failed ({(int)resp.StatusCode}): {body}");

        if (resp.Headers.Location is null)
            throw new InvalidOperationException("Keycloak create user missing Location header");

        _logger.LogInformation("Keycloak create user successful: \n{User}", rep.ToString());

        return resp.Headers.Location.ToString().Split('/').Last();
    }

    private async Task UpdateUserAsync(
        string realm,
        string userId,
        string? providerId,
        string username,
        string email,
        CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users/{userId}";

        // Fetch current user to preserve immutable attributes (e.g. userId)
        var current = await GetUserAsync(realm, userId, ct);

        var tenantId = ResolveTenantId(providerId);
        var role = providerId is not null ? "provider" : "user";

        current.Attributes ??= new Dictionary<string, string[]>();

        // providerId (optional)
        if (!string.IsNullOrWhiteSpace(providerId))
        {
            current.Attributes[_opts.ProviderIdAttributeName] = new[] { providerId };
        }
        else
        {
            current.Attributes.Remove(_opts.ProviderIdAttributeName);
        }

        // tenantId (always set)
        current.Attributes[_opts.TenantIdAttributeName] = new[] { tenantId };

        // role (always set)
        current.Attributes[_opts.UserRole] = new[] { role };

        var rep = new UserRep
        {
            Id = userId,
            Username = username,
            Enabled = true,
            Email = email,
            EmailVerified = _opts.RequireEmailVerified,
            Attributes = current.Attributes
        };

        using var resp = await _http.PutAsJsonAsync(url, rep, JsonOpts, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Keycloak update user failed ({(int)resp.StatusCode}): {body}");
    }


    private sealed class UserRep
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, string[]>? Attributes { get; set; }
    }

    // -------- password / required actions --------
    private async Task SetPasswordAsync(string realm, string userId, string password, bool temporary, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users/{userId}/reset-password";
        var payload = new { type = "password", value = password, temporary };
        using var resp = await _http.PutAsJsonAsync(url, payload, JsonOpts, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak set password failed ({(int)resp.StatusCode}): {body}");
    }

    private async Task SetRequiredActionsAsync(string realm, string userId, string[] actions, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users/{userId}";

        // Minimal put update: fetch current, then update requiredActions
        var current = await GetUserAsync(realm, userId, ct);

        var payload = new
        {
            id = userId,
            username = current.Username,
            enabled = current.Enabled,
            email = current.Email,
            emailVerified = current.EmailVerified,
            attributes = current.Attributes,
            requiredActions = actions
        };

        using var resp = await _http.PutAsJsonAsync(url, payload, JsonOpts, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak set requiredActions failed ({(int)resp.StatusCode}): {body}");
    }

    private async Task<UserRep> GetUserAsync(string realm, string userId, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users/{userId}";
        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak get user failed ({(int)resp.StatusCode}): {body}");

        return JsonSerializer.Deserialize<UserRep>(body, JsonOpts)
               ?? throw new InvalidOperationException("Keycloak user parse failed");
    }

    // -------- roles --------
    private async Task EnsureRealmRoleAssignedAsync(string realm, string userId, string roleName, CancellationToken ct)
    {
        var role = await GetRealmRoleAsync(realm, roleName, ct);

        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/realm";
        using var resp = await _http.PostAsJsonAsync(url, new[] { role }, JsonOpts, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak assign role failed ({(int)resp.StatusCode}): {body}");
    }

    private async Task<RoleRep> GetRealmRoleAsync(string realm, string roleName, CancellationToken ct)
    {
        var url = $"{_opts.BaseUrl}/admin/realms/{realm}/roles/{Uri.EscapeDataString(roleName)}";
        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Keycloak get role failed ({(int)resp.StatusCode}): {body}");

        return JsonSerializer.Deserialize<RoleRep>(body, JsonOpts)
               ?? throw new InvalidOperationException("Keycloak role parse failed");
    }

    private sealed class RoleRep
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
