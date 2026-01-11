namespace UserService.Infrastructure.Keycloak;

public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080";

    // Where the admin user lives (usually master)
    public string AdminRealm { get; set; } = "master";

    // Target realm where you create provider users
    public string Realm { get; set; } = "powerport";

    // MVP: admin username/password (later switch to client_credentials)
    public string AdminClientId { get; set; } = "admin-cli";
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";

    // Role + attribute conventions
    public string ProviderRoleName { get; set; } = "Provider";
    public string TenantIdAttributeName { get; set; } = "tenantId";


    // Enforce change password on first login
    public bool ForcePasswordUpdateOnFirstLogin { get; set; } = true;

    // If you require email verification for providers
    public bool RequireEmailVerified { get; set; } = false;
}
