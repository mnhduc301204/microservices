using ECommerce.Payment.Data;

namespace ECommerce.Payment.Features.HandlePaymentWebhook;

public static class PaymentWebhookEndpoint
{
    public static RouteGroupBuilder MapPaymentWebhook(this RouteGroupBuilder group)
    {
        group.MapPost("/webhooks/fake-provider", async (
            PaymentWebhookCommand command,
            PaymentDbContext dbContext,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken cancellationToken) =>
            await new PaymentWebhookHandler(dbContext, configuration, environment).Handle(command, cancellationToken));

        return group;
    }
}
