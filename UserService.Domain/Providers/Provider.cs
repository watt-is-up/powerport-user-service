namespace UserService.Domain.Providers;

public sealed class Provider
{
    public string UniqueName { get; }
    public string DisplayName { get; }
    public string ProviderEmail { get; }
    public Guid TenantId { get; }
    public DateTimeOffset CreatedAt { get; }

    public Provider(string uniqueName, string displayName, string providerEmail, Guid tenantId)
    {
        UniqueName = (uniqueName ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(UniqueName))
            throw new ArgumentException("uniqueName is required", nameof(uniqueName));
        if (UniqueName.Length < 2)
            throw new ArgumentException("uniqueName too short", nameof(uniqueName));

        DisplayName = (displayName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new ArgumentException("displayName is required", nameof(displayName));

        ProviderEmail = (providerEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ProviderEmail))
            throw new ArgumentException("providerEmail is required", nameof(providerEmail));

        TenantId = tenantId; // <-- YOU WERE MISSING THIS
        CreatedAt = DateTimeOffset.UtcNow;
    }
}