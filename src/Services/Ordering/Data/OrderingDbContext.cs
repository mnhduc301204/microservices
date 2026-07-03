using ECommerce.Ordering.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Data;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<CheckoutSagaState> CheckoutSagas => Set<CheckoutSagaState>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(order => order.Id);
            builder.Property(order => order.CustomerEmail).HasMaxLength(320).IsRequired();
            builder.Property(order => order.Total).HasPrecision(18, 2);
            builder.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(64).IsRequired();
            builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProcessedMessage>(builder => builder.HasKey(message => message.EventId));

        modelBuilder.Entity<CheckoutSagaState>(builder =>
        {
            builder.HasKey(saga => saga.CheckoutId);
            builder.HasIndex(saga => saga.OrderId).IsUnique();
            builder.Property(saga => saga.CurrentStep).HasMaxLength(120).IsRequired();
            builder.Property(saga => saga.FailureReason).HasMaxLength(500);
        });

        modelBuilder.AddInboxOutboxEntities();
    }
}
