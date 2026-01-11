namespace UserService.Application.Configuration;
public sealed class TenantProvisioningOptions
{
    public string Environment { get; init; } = "dev";
    public string PostgresHost { get; init; } = ""; // pg-powerport-dev.postgres.database.azure.com
    public int PostgresPort { get; init; } = 5432;

    // admin creds (MVP). Later replace with Managed Identity + AAD auth if possible.
    public string AdminUser { get; init; } = "";     // often needs user@serverName
    public string AdminPassword { get; init; } = "";

    public string KeyVaultUri { get; init; } = "";   // https://kv-powerport-dev.vault.azure.net/
    public string[] TenantServices { get; init; } = new[] { "billing", "stationmgmt", "provider", "tracking", "reviews" };
}
