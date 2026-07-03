using Microsoft.EntityFrameworkCore;

namespace ECommerce.ServiceDefaults.Messaging;

public static class InboxExtensions
{
    public static Task<bool> HasProcessedAsync(
        this DbContext dbContext,
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<InboxMessage>()
            .AnyAsync(message => message.EventId == eventId && message.Consumer == consumer, cancellationToken);
    }

    public static void MarkProcessed(this DbContext dbContext, Guid eventId, string consumer)
    {
        dbContext.Set<InboxMessage>().Add(InboxMessage.Create(eventId, consumer));
    }
}
