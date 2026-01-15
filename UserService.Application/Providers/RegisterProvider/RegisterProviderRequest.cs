namespace UserService.Application.Providers.RegisterProvider;

public sealed class RegisterProviderRequest
{
    public Guid ProviderId { get; set; } = default!;
    public string DisplayName { get; set; } = "";
}
