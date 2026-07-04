using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.ServiceDefaults.Messaging;

public sealed class OutboxDispatcher<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<OutboxDispatcher<TDbContext>> logger)
    : BackgroundService
    where TDbContext : DbContext
{
    private readonly string workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchBatch(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task DispatchBatch(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<KafkaOutboxPublisher>();
        var now = DateTimeOffset.UtcNow;
        var staleLockCutoff = now.AddMinutes(-5);

        var candidateIds = await dbContext.Set<OutboxMessage>()
            .Where(message =>
                message.ProcessedAt == null
                && !message.IsDeadLetter
                && (message.LockedAt == null || message.LockedAt <= staleLockCutoff)
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.OccurredAt)
            .Take(20)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return;
        }

        await dbContext.Set<OutboxMessage>()
            .Where(message =>
                candidateIds.Contains(message.Id)
                && message.ProcessedAt == null
                && !message.IsDeadLetter
                && (message.LockedAt == null || message.LockedAt <= staleLockCutoff)
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.LockedAt, now)
                .SetProperty(message => message.LockedBy, workerId), cancellationToken);

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(message =>
                candidateIds.Contains(message.Id)
                && message.LockedBy == workerId
                && message.ProcessedAt == null
                && !message.IsDeadLetter)
            .OrderBy(message => message.OccurredAt)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.Publish(message, cancellationToken);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish outbox message {MessageId} to {Topic}", message.Id, message.Topic);
                message.MarkFailed(ex);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class KafkaOutboxPublisher(IServiceProvider serviceProvider)
{
    public async Task Publish(OutboxMessage message, CancellationToken cancellationToken)
    {
        var messageType = Type.GetType(message.MessageType)
            ?? throw new InvalidOperationException($"Message type '{message.MessageType}' could not be loaded.");

        var payload = JsonSerializer.Deserialize(message.Payload, messageType, MessagingJson.Options)
            ?? throw new InvalidOperationException($"Message payload '{message.Id}' could not be deserialized.");

        if (!string.IsNullOrWhiteSpace(message.PartitionKey))
        {
            var keyedProducerType = typeof(ITopicProducer<,>).MakeGenericType(typeof(string), messageType);
            var keyedProducer = serviceProvider.GetService(keyedProducerType);
            if (keyedProducer is not null)
            {
                var keyedProduceMethod = keyedProducerType.GetMethod(nameof(ITopicProducer<string, object>.Produce), [typeof(string), messageType, typeof(CancellationToken)])
                    ?? throw new InvalidOperationException($"Keyed Produce method for '{messageType.Name}' could not be found.");

                var keyedTask = (Task)keyedProduceMethod.Invoke(keyedProducer, [message.PartitionKey, payload, cancellationToken])!;
                await keyedTask;
                return;
            }
        }

        var producerType = typeof(ITopicProducer<>).MakeGenericType(messageType);
        var producer = serviceProvider.GetRequiredService(producerType);
        var produceMethod = producerType.GetMethod(nameof(ITopicProducer<object>.Produce), [messageType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"Produce method for '{messageType.Name}' could not be found.");
        var task = (Task)produceMethod.Invoke(producer, [payload, cancellationToken])!;
        await task;
    }
}
