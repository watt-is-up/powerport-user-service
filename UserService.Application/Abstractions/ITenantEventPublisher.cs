namespace UserService.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, string eventType, string key, T payload, CancellationToken ct);
}
