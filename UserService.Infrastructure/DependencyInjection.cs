using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions;
using UserService.Application.Providers.RegisterProvider;
using UserService.Infrastructure.Kafka;
using UserService.Infrastructure.Keycloak;
using UserService.Infrastructure.Providers;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<Application.Abstractions.KafkaOptions>(cfg.GetSection("Kafka"));
        services.Configure<KeycloakOptions>(cfg.GetSection("Keycloak"));

        // MVP repo (swap with EF Core later)
        services.AddSingleton<IProviderRepository, InMemoryProviderRepository>();

        // Kafka publisher
        services.AddSingleton<IEventPublisher, KafkaEventProducer>();

        // Keycloak admin API client
        services.AddHttpClient<IKeycloakProvisioningClient, KeycloakProvisioningClient>();

        services.AddScoped<ITenantInfraProvisioner, TenantInfraProvisioner>();
        return services;
    }
}
