namespace UserService.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:19092";
    public string ProducerName { get; set; } = "users-service";
    public string TenantEventsTopic { get; set; } = "tenant.events";
}

