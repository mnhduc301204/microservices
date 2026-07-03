using Microsoft.EntityFrameworkCore;

namespace ECommerce.ServiceDefaults.Messaging;

public static class InboxExtensions
{
    private static readonly string WorkerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public static Task<bool> HasProcessedAsync(
        this DbContext dbContext,
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<InboxMessage>()
            .AnyAsync(message => message.EventId == eventId && message.Consumer == consumer, cancellationToken);
    }

    public static async Task<bool> TryBeginProcessingAsync(
        this DbContext dbContext,
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(message => message.EventId == eventId && message.Consumer == consumer, cancellationToken);

        if (existing is null)
        {
            dbContext.Set<InboxMessage>().Add(InboxMessage.CreateProcessing(eventId, consumer, WorkerId));
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
                return false;
            }
        }

        if (existing.Status == InboxMessageStatus.Processed)
        {
            return false;
        }

        var staleLockCutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        if (existing.Status == InboxMessageStatus.Processing && existing.LockedAt > staleLockCutoff)
        {
            return false;
        }

        existing.Claim(WorkerId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static void MarkProcessed(this DbContext dbContext, Guid eventId, string consumer)
    {
        var entry = dbContext.ChangeTracker.Entries<InboxMessage>()
            .FirstOrDefault(entry => entry.Entity.EventId == eventId && entry.Entity.Consumer == consumer);

        if (entry is not null)
        {
            entry.Entity.MarkProcessed();
            return;
        }

        dbContext.Attach(InboxMessage.CreateProcessing(eventId, consumer, WorkerId)).Entity.MarkProcessed();
    }

    public static void MarkFailed(this DbContext dbContext, Guid eventId, string consumer, Exception exception)
    {
        var entry = dbContext.ChangeTracker.Entries<InboxMessage>()
            .FirstOrDefault(entry => entry.Entity.EventId == eventId && entry.Entity.Consumer == consumer);

        if (entry is not null)
        {
            entry.Entity.MarkFailed(exception);
        }
    }
}
