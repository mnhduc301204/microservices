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

        var reservationIds = await dbContext.Reservations
            .Where(reservation =>
                reservation.Status == StockReservationStatus.Reserved
                && reservation.ExpiresAt <= now)
            .OrderBy(reservation => reservation.ExpiresAt)
            .Take(100)
            .Select(reservation => reservation.Id)
            .ToListAsync(cancellationToken);

        foreach (var reservationId in reservationIds)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var reservation = await dbContext.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.Id == reservationId
                        && candidate.Status == StockReservationStatus.Reserved
                        && candidate.ExpiresAt <= now,
                    cancellationToken);

            if (reservation is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            var claimed = await dbContext.Reservations
                .Where(candidate => candidate.Id == reservation.Id && candidate.Status == StockReservationStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, StockReservationStatus.Released)
                    .SetProperty(candidate => candidate.CompletedAt, DateTimeOffset.UtcNow), cancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            var item = await dbContext.Items.FirstAsync(item => item.Sku == reservation.Sku, cancellationToken);
            item.Release(reservation.Quantity);
            dbContext.StockMovements.Add(new StockMovement(
                reservation.Sku,
                reservation.Quantity,
                StockMovementType.ReservationExpired,
                reservation.ReservationId,
                reservation.OrderId,
                "Reservation expired."));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
