using ECommerce.Payment.Data;

namespace ECommerce.Payment.Features.ConfirmPayment;

public static class ConfirmPaymentEndpoint
{
    public static RouteGroupBuilder MapConfirmPayment(this RouteGroupBuilder group)
    {
        group.MapPost("/{paymentId:guid}/confirm", async (Guid paymentId, ConfirmPaymentCommand command, PaymentDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (paymentId != command.PaymentId)
            {
                return Results.BadRequest(new { error = "Route paymentId does not match command paymentId." });
            }

            return await new ConfirmPaymentHandler(dbContext).Handle(command, cancellationToken);
        });

        return group;
    }
}
