namespace UserService.Application.Providers.RegisterProvider;

public sealed class ProvisioningOptions
{
    public string ProviderAdminUsernameSuffix { get; set; } = "-admin";
    public string ProviderAdminEmailDomain { get; set; } = "local.test";
    public int TemporaryPasswordLength { get; set; } = 16;
}
