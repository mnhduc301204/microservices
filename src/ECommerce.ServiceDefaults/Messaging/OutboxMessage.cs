using System.Text.Json;

namespace ECommerce.ServiceDefaults.Messaging;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string Topic { get; private set; } = string.Empty;

    public string MessageType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public string? PartitionKey { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? LockedAt { get; private set; }

    public string? LockedBy { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public int AttemptCount { get; private set; }

    public bool IsDeadLetter { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create<T>(string topic, T message)
        where T : notnull
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
            Payload = JsonSerializer.Serialize(message, MessagingJson.Options),
            PartitionKey = ResolvePartitionKey(message),
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }

    private static string? ResolvePartitionKey<T>(T message)
        where T : notnull
    {
        string[] candidateNames =
        [
            "OrderId",
            "Sku",
            "CustomerId",
            "PaymentId",
            "ReservationId",
            "ProductId",
            "EventId",
        ];

        var messageType = typeof(T);
        foreach (var candidateName in candidateNames)
        {
            var property = messageType.GetProperty(candidateName);
            var value = property?.GetValue(message);
            if (value is not null)
            {
                return value.ToString();
            }
        }

        return null;
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTimeOffset.UtcNow;
        LockedAt = null;
        LockedBy = null;
        NextAttemptAt = null;
        Error = null;
    }

    public void MarkFailed(Exception exception)
    {
        AttemptCount++;
        Error = exception.Message;
        LockedAt = null;
        LockedBy = null;
        IsDeadLetter = AttemptCount >= 10;
        NextAttemptAt = IsDeadLetter
            ? null
            : DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, AttemptCount)));
    }

    public void Lock(string workerId)
    {
        LockedAt = DateTimeOffset.UtcNow;
        LockedBy = workerId;
    }
}
