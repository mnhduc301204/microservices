using ECommerce.Catalog.Features.ChangeProductStatus;
using ECommerce.Catalog.Features.CreateProduct;
using ECommerce.Catalog.Features.GetProduct;
using ECommerce.Catalog.Features.SearchProducts;
using ECommerce.Catalog.Features.UpdateProduct;

namespace ECommerce.Catalog.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog/products").WithTags("Catalog");

        group.MapCreateProduct();
        group.MapUpdateProduct();
        group.MapGetProduct();
        group.MapSearchProducts();
        group.MapChangeProductStatus();

        return app;
    }
}
