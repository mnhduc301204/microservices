using ECommerce.Payment.Features.ConfirmPayment;
using ECommerce.Payment.Features.CreatePayment;
using ECommerce.Payment.Features.HandlePaymentWebhook;
using ECommerce.Payment.Features.RefundPayment;

namespace ECommerce.Payment.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payment");

        group.MapCreatePayment();
        group.MapConfirmPayment();
        group.MapRefundPayment();
        group.MapPaymentWebhook();

        return app;
    }
}
