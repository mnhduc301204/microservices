namespace ECommerce.Notification.Models;

public sealed class NotificationRecord
{
    private NotificationRecord()
    {
    }

    public NotificationRecord(string type, string recipient, string message, Guid? sourceEventId)
    {
        Type = string.IsNullOrWhiteSpace(type) ? throw new ArgumentException("Notification type is required.", nameof(type)) : type.Trim();
        Recipient = string.IsNullOrWhiteSpace(recipient) ? throw new ArgumentException("Recipient is required.", nameof(recipient)) : recipient.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Message is required.", nameof(message)) : message.Trim();
        SourceEventId = sourceEventId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Type { get; private set; } = string.Empty;

    public string Recipient { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public Guid? SourceEventId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
