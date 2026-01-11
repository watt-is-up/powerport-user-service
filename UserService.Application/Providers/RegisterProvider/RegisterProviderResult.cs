namespace UserService.Application.Providers.RegisterProvider;

public sealed record RegisterProviderResult(
    string UniqueName,
    Guid TenantId,
    string DisplayName,
    string AdminUsername,
    string AdminEmail,
    string TemporaryPassword
);
