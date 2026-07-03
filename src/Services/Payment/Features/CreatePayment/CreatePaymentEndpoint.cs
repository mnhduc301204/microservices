using ECommerce.Payment.Data;

namespace ECommerce.Payment.Features.CreatePayment;

public static class CreatePaymentEndpoint
{
    public static RouteGroupBuilder MapCreatePayment(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreatePaymentCommand command,
            PaymentDbContext dbContext,
            HttpRequest request,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();
            return await new CreatePaymentHandler(dbContext).Handle(command, idempotencyKey, cancellationToken);
        });

        return group;
    }
}
