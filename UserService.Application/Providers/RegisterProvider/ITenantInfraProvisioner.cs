namespace UserService.Application.Providers.RegisterProvider
{
    public interface ITenantInfraProvisioner
    {
        Task<TenantInfraResult> EnsureTenantDatabasesAsync(
            string uniqueName,
            string environment,
            CancellationToken ct);
    }

    public sealed record TenantInfraResult(
        Dictionary<string, string> DatabaseNames,
        Dictionary<string, string> ConnectionSecretNames);
}
