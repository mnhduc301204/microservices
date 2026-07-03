namespace ECommerce.Notification.Models;

public sealed class ProcessedMessage
{
    public Guid EventId { get; init; }

    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}
