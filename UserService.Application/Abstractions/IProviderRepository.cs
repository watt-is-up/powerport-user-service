using UserService.Domain.Providers;

namespace UserService.Application.Abstractions;

public interface IProviderRepository
{
    Task<bool> ExistsAsync(string providerId, CancellationToken ct);
    Task CreateAsync(Provider provider, CancellationToken ct);
}
