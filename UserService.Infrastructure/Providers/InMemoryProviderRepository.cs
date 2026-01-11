using System.Collections.Concurrent;
using UserService.Application.Abstractions;
using UserService.Domain.Providers;

namespace UserService.Infrastructure.Providers;

public sealed class InMemoryProviderRepository : IProviderRepository
{
    private readonly ConcurrentDictionary<string, Provider> _store = new();

    public Task<bool> ExistsAsync(string uniqueName, CancellationToken ct)
        => Task.FromResult(_store.ContainsKey(Norm(uniqueName)));

    public Task CreateAsync(Provider provider, CancellationToken ct)
    {
        if (!_store.TryAdd(Norm(provider.UniqueName), provider))
            throw new InvalidOperationException($"Provider '{provider.UniqueName}' already exists.");
        return Task.CompletedTask;
    }

    private static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant();
}