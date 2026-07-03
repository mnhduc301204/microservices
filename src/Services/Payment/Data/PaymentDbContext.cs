using ECommerce.Payment.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Data;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Models.Payment> Payments => Set<Models.Payment>();

    public DbSet<PaymentWebhookEvent> WebhookEvents => Set<PaymentWebhookEvent>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Payment>(builder =>
        {
            builder.HasKey(payment => payment.Id);
            builder.Property(payment => payment.Amount).HasPrecision(18, 2);
            builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
            builder.Property(payment => payment.IdempotencyKey).HasMaxLength(200);
            builder.Property(payment => payment.ProviderTransactionId).HasMaxLength(200);
            builder.Property(payment => payment.FailureReason).HasMaxLength(500);
            builder.HasIndex(payment => payment.OrderId).IsUnique();
            builder.HasIndex(payment => payment.IdempotencyKey).IsUnique();
            builder.HasIndex(payment => payment.ProviderTransactionId).IsUnique();
        });

        modelBuilder.Entity<PaymentWebhookEvent>(builder =>
        {
            builder.HasKey(webhook => webhook.EventId);
            builder.Property(webhook => webhook.ProviderTransactionId).HasMaxLength(200).IsRequired();
            builder.Property(webhook => webhook.Status).HasMaxLength(40).IsRequired();
            builder.HasIndex(webhook => webhook.ProviderTransactionId);
        });

        modelBuilder.Entity<ProcessedMessage>(builder => builder.HasKey(message => message.EventId));

        modelBuilder.AddInboxOutboxEntities();
    }
}
