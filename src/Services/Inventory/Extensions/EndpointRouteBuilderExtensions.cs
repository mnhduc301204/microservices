using ECommerce.Inventory.Features.DeductStock;
using ECommerce.Inventory.Features.GetStock;
using ECommerce.Inventory.Features.ReleaseReservation;
using ECommerce.Inventory.Features.ReserveStock;

namespace ECommerce.Inventory.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/items").WithTags("Inventory");

        group.MapGetStock();
        group.MapReserveStock();
        group.MapReleaseReservation();
        group.MapDeductStock();

        return app;
    }
}
