namespace ECommerce.ServiceDefaults.Messaging;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public IdempotencyRecord(string serviceName, string idempotencyKey, int statusCode, string? responseBody)
    {
        ServiceName = string.IsNullOrWhiteSpace(serviceName) ? throw new ArgumentException("Service name is required.", nameof(serviceName)) : serviceName.Trim();
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey)) : idempotencyKey.Trim();
        StatusCode = statusCode;
        ResponseBody = responseBody;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string ServiceName { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public int StatusCode { get; private set; }

    public string? ResponseBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
