using UserService.Domain.Providers;

namespace UserService.Application.Abstractions;

public interface IProviderRepository
{
    Task<bool> ExistsAsync(string uniqueName, CancellationToken ct);
    Task CreateAsync(Provider provider, CancellationToken ct);
}