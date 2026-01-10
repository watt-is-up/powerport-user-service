namespace UserService.Domain.Providers;

public sealed class Provider
{
    public string ProviderId { get; }
    public string DisplayName { get; }
    public DateTimeOffset CreatedAt { get; }

    public Provider(string providerId, string displayName)
    {
        ProviderId = (providerId ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ProviderId))
            throw new ArgumentException("providerId is required", nameof(providerId));
        if (ProviderId.Length < 2)
            throw new ArgumentException("providerId too short", nameof(providerId));

        DisplayName = (displayName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new ArgumentException("displayName is required", nameof(displayName));

        CreatedAt = DateTimeOffset.UtcNow;
    }
}
