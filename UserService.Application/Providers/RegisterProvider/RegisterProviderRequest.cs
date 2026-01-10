namespace UserService.Application.Providers.RegisterProvider;

public sealed class RegisterProviderRequest
{
    public string ProviderId { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
