namespace ECommerce.ServiceDefaults.Messaging;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public Guid EventId { get; private set; }

    public string Consumer { get; private set; } = string.Empty;

    public InboxMessageStatus Status { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? LockedAt { get; private set; }

    public string? LockedBy { get; private set; }

    public int AttemptCount { get; private set; }

    public string? Error { get; private set; }

    public static InboxMessage CreateProcessing(Guid eventId, string consumer, string workerId)
    {
        return new InboxMessage
        {
            EventId = eventId,
            Consumer = consumer,
            Status = InboxMessageStatus.Processing,
            ReceivedAt = DateTimeOffset.UtcNow,
            LockedAt = DateTimeOffset.UtcNow,
            LockedBy = workerId,
        };
    }

    public void Claim(string workerId)
    {
        Status = InboxMessageStatus.Processing;
        LockedAt = DateTimeOffset.UtcNow;
        LockedBy = workerId;
        Error = null;
    }

    public void MarkProcessed()
    {
        Status = InboxMessageStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
        LockedAt = null;
        LockedBy = null;
        Error = null;
    }

    public void MarkFailed(Exception exception)
    {
        Status = InboxMessageStatus.Failed;
        AttemptCount++;
        LockedAt = null;
        LockedBy = null;
        Error = exception.Message;
    }
}

public enum InboxMessageStatus
{
    Processing = 1,
    Processed = 2,
    Failed = 3,
}
