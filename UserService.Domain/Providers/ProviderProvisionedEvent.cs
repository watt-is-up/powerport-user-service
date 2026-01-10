namespace UserService.Domain.Providers;

public sealed class ProviderProvisioned
{
    public string ProviderId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Environment { get; set; } = default!;
    public List<string> TenantServices { get; set; } = new();

    // Optional: helpful for ops
    public string KeycloakAdminUsername { get; set; } = default!;
    public string KeycloakAdminEmail { get; set; } = default!;
}
