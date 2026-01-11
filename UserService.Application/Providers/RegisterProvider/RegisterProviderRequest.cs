namespace UserService.Application.Providers.RegisterProvider;

public sealed class RegisterProviderRequest
{
    public string UniqueName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ProviderEmail { get; set; } = "";
}

