using ECommerce.Inventory.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Data;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    public DbSet<StockReservation> Reservations => Set<StockReservation>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(builder =>
        {
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Sku).HasMaxLength(64).IsRequired();
            builder.HasIndex(item => item.Sku).IsUnique();
        });

        modelBuilder.Entity<StockReservation>(builder =>
        {
            builder.HasKey(reservation => reservation.Id);
            builder.Property(reservation => reservation.Sku).HasMaxLength(64).IsRequired();
            builder.HasIndex(reservation => reservation.ReservationId).IsUnique();
            builder.HasIndex(reservation => new { reservation.Status, reservation.ExpiresAt });
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.HasKey(movement => movement.Id);
            builder.Property(movement => movement.Sku).HasMaxLength(64).IsRequired();
            builder.Property(movement => movement.Reason).HasMaxLength(300).IsRequired();
            builder.HasIndex(movement => new { movement.Sku, movement.CreatedAt });
            builder.HasIndex(movement => movement.ReservationId);
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.HasKey(message => message.EventId);
        });

        modelBuilder.AddInboxOutboxEntities();
    }
}
