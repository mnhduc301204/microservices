using ECommerce.Ordering.Features.CancelOrder;
using ECommerce.Ordering.Features.CreateOrder;
using ECommerce.Ordering.Features.GetOrder;
using ECommerce.Ordering.Features.MarkOrderPaymentSucceeded;

namespace ECommerce.Ordering.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Ordering");

        group.MapCreateOrder();
        group.MapGetOrder();
        group.MapCancelOrder();
        group.MapMarkOrderPaymentSucceeded();

        return app;
    }
}
