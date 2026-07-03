using ECommerce.Payment.Data;

namespace ECommerce.Payment.Features.RefundPayment;

public static class RefundPaymentEndpoint
{
    public static RouteGroupBuilder MapRefundPayment(this RouteGroupBuilder group)
    {
        group.MapPost("/{paymentId:guid}/refund", async (
            Guid paymentId,
            PaymentDbContext dbContext,
            HttpRequest request,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();
            return await new RefundPaymentHandler(dbContext).Handle(new RefundPaymentCommand(paymentId), idempotencyKey, cancellationToken);
        });

        return group;
    }
}
