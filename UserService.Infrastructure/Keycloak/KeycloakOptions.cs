namespace UserService.Infrastructure.Keycloak;

public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; } = default!;

    // Where the admin user lives (usually master)
    public string AdminRealm { get; set; } = default!;

    // Target realm where you create provider users
    public string Realm { get; set; } = default!;

    // MVP: admin username/password (later switch to client_credentials)
    public string AdminClientId { get; set; } = default!;
    public string AdminUsername { get; set; } = default!;
    public string AdminPassword { get; set; } = default!;

    // Role + attribute conventions
    public string UserRole { get; set; } = default!;
    public string ProviderIdAttributeName { get; set; } = default!;
    public string TenantIdAttributeName { get; set; } = default!;

    public string DefaultTenantId { get; set; } = "11111111-1111-1111-1111-111111111111";

    // Enforce change password on first login
    public bool ForcePasswordUpdateOnFirstLogin { get; set; } = true;

    // If you require email verification for providers
    public bool RequireEmailVerified { get; set; } = false;
}
