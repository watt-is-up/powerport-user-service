namespace UserService.Application.Abstractions;

public interface IKeycloakProvisioningClient
{
    /// <summary>
    /// Ensures the provider admin user exists in Keycloak, has Provider role, provider_id attribute, and a temporary password.
    /// </summary>
    Task EnsureProviderAdminUserAsync(
    string uniqueName,
    string displayName,
    string adminUsername,
    string adminEmail,
    string tenantId,
    string temporaryPassword,
    CancellationToken ct);

}
