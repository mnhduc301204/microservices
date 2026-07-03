using ECommerce.Ordering.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Data;

public static class OrderingStateTransitions
{
    public static Task<int> TryConfirmOrderAsync(this OrderingDbContext dbContext, Guid orderId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.Orders
            .Where(order => order.Id == orderId && order.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatus.Confirmed)
                .SetProperty(order => order.ConfirmedAt, now), cancellationToken);
    }

    public static Task<int> TryFailOrderAsync(this OrderingDbContext dbContext, Guid orderId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.Orders
            .Where(order => order.Id == orderId && order.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatus.Failed)
                .SetProperty(order => order.CancelledAt, now), cancellationToken);
    }

    public static Task<int> TryMarkSagaStockReservedAsync(
        this OrderingDbContext dbContext,
        Guid orderId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.CheckoutSagas
            .Where(saga => saga.OrderId == orderId && saga.Status == CheckoutSagaStatus.OrderCreated)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(saga => saga.StockReservationId, reservationId)
                .SetProperty(saga => saga.Status, CheckoutSagaStatus.StockReserved)
                .SetProperty(saga => saga.CurrentStep, "StockReserved")
                .SetProperty(saga => saga.UpdatedAt, now), cancellationToken);
    }

    public static Task<int> TryCompleteSagaAsync(
        this OrderingDbContext dbContext,
        Guid orderId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.CheckoutSagas
            .Where(saga => saga.OrderId == orderId && saga.Status != CheckoutSagaStatus.Completed && saga.Status != CheckoutSagaStatus.Failed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(saga => saga.PaymentId, paymentId)
                .SetProperty(saga => saga.Status, CheckoutSagaStatus.Completed)
                .SetProperty(saga => saga.CurrentStep, "PaymentSucceeded")
                .SetProperty(saga => saga.UpdatedAt, now)
                .SetProperty(saga => saga.CompletedAt, now), cancellationToken);
    }

    public static Task<int> TryFailSagaAsync(
        this OrderingDbContext dbContext,
        Guid orderId,
        string reason,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var failureReason = string.IsNullOrWhiteSpace(reason) ? "Checkout failed." : reason.Trim();
        return dbContext.CheckoutSagas
            .Where(saga => saga.OrderId == orderId && saga.Status != CheckoutSagaStatus.Completed && saga.Status != CheckoutSagaStatus.Failed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(saga => saga.Status, CheckoutSagaStatus.Failed)
                .SetProperty(saga => saga.CurrentStep, "Failed")
                .SetProperty(saga => saga.FailureReason, failureReason)
                .SetProperty(saga => saga.UpdatedAt, now)
                .SetProperty(saga => saga.CompletedAt, now), cancellationToken);
    }
}
