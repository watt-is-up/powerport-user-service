namespace UserService.Domain.Providers;

public sealed class EventEnvelope<T>
{
    public string EventId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public int EventVersion { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Producer { get; set; } = default!;
    public string Key { get; set; } = default!;
    public T Payload { get; set; } = default!;
}
