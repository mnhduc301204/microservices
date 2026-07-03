using Microsoft.EntityFrameworkCore;

namespace ECommerce.ServiceDefaults.Messaging;

public static class MessagingModelBuilderExtensions
{
    public static ModelBuilder AddInboxOutboxEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property(message => message.Topic).HasMaxLength(200).IsRequired();
            builder.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            builder.Property(message => message.Payload).IsRequired();
            builder.Property(message => message.Error).HasMaxLength(2000);
            builder.Property(message => message.LockedBy).HasMaxLength(200);
            builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt, message.IsDeadLetter });
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.HasKey(message => new { message.EventId, message.Consumer });
            builder.Property(message => message.Consumer).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.HasKey(record => record.Id);
            builder.Property(record => record.ServiceName).HasMaxLength(120).IsRequired();
            builder.Property(record => record.IdempotencyKey).HasMaxLength(200).IsRequired();
            builder.Property(record => record.ResponseBody);
            builder.HasIndex(record => new { record.ServiceName, record.IdempotencyKey }).IsUnique();
        });

        return modelBuilder;
    }
}
