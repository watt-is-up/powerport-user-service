using System.Collections.Concurrent;
using UserService.Application.Abstractions;
using UserService.Domain.Providers;

namespace UserService.Infrastructure.Providers;

public sealed class InMemoryProviderRepository : IProviderRepository
{
    private readonly ConcurrentDictionary<string, Provider> _store = new();

    public Task<bool> ExistsAsync(string providerId, CancellationToken ct)
        => Task.FromResult(_store.ContainsKey(providerId));

    public Task CreateAsync(Provider provider, CancellationToken ct)
    {
        if (!_store.TryAdd(provider.ProviderId, provider))
            throw new InvalidOperationException($"Provider '{provider.ProviderId}' already exists.");
        return Task.CompletedTask;
    }
}
