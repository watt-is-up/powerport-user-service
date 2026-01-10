namespace UserService.Application.Providers.RegisterProvider;

public interface IProviderProvisioningService
{
    Task<RegisterProviderResult> RegisterProviderAsync(RegisterProviderRequest request, CancellationToken ct);
}
