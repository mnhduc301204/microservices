namespace ECommerce.ServiceDefaults.Messaging;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public Guid EventId { get; private set; }

    public string Consumer { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; private set; }

    public static InboxMessage Create(Guid eventId, string consumer)
    {
        return new InboxMessage
        {
            EventId = eventId,
            Consumer = consumer,
            ProcessedAt = DateTimeOffset.UtcNow,
        };
    }
}
