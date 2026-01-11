namespace UserService.Domain.Providers
{
    public sealed class ProviderProvisionedV2
    {
        public Guid TenantId { get; init; }
        public string ProviderUniqueName { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Environment { get; init; } = "dev";

        // Prefer secrets over db names (services can fetch conn string via secret name)
        public Dictionary<string, string> ConnectionSecretNames { get; init; } = new();

        // Optional if you want
        public Dictionary<string, string>? DatabaseNames { get; init; }
    }
}