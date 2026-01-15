namespace UserService.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = default!;
    public string ProducerName { get; set; } = default!;
    public string UserServiceTopic { get; set; } = default!;
}

