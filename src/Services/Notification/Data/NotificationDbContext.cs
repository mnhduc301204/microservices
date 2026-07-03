using ECommerce.Notification.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notification.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(builder =>
        {
            builder.HasKey(notification => notification.Id);
            builder.Property(notification => notification.Type).HasMaxLength(120).IsRequired();
            builder.Property(notification => notification.Recipient).HasMaxLength(320).IsRequired();
            builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<ProcessedMessage>(builder => builder.HasKey(message => message.EventId));

        modelBuilder.AddInboxOutboxEntities();
    }
}
