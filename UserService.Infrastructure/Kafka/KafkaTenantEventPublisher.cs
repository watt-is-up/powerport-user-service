using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using UserService.Application.Abstractions;
using UserService.Domain.Providers;

namespace UserService.Infrastructure.Kafka;

public sealed class KafkaEventProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _producerName;

    public KafkaEventProducer(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        _producerName = config["Kafka:ProducerName"] ?? "users-service";
    }

    public async Task PublishAsync<T>(string topic, string eventType, string key, T payload, CancellationToken ct)
    {
        var envelope = new EventEnvelope<T>
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            EventVersion = 1,
            OccurredAt = DateTime.UtcNow,
            Producer = _producerName,
            Key = key,
            Payload = payload
        };

        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(envelope)
            },
            ct);
    }

    public void Dispose()
    {
        try { _producer.Flush(TimeSpan.FromSeconds(3)); } catch { }
        _producer.Dispose();
    }
}
