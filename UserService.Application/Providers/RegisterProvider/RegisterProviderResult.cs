namespace UserService.Application.Providers.RegisterProvider;

public sealed record RegisterProviderResult(
    string ProviderId,
    string DisplayName,
    string AdminUsername,
    string AdminEmail,
    string TemporaryPassword
);
