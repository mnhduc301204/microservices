using ECommerce.Inventory.Data;
using ECommerce.Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.BackgroundServices;

public sealed class ReservationExpiryService(
    IServiceProvider serviceProvider,
    ILogger<ReservationExpiryService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservations(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to expire stock reservations.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ExpireReservations(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var now = DateTimeOffset.UtcNow;

        var reservations = await dbContext.Reservations
            .Where(reservation => reservation.Status == StockReservationStatus.Reserved && reservation.ExpiresAt <= now)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, cancellationToken);
            item.Release(reservation.Quantity);
            reservation.MarkReleased();
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.ReservationExpired,
                reservation.ReservationId,
                reservation.OrderId,
                "Reservation expired."));
        }

        if (reservations.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
