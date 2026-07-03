namespace ECommerce.Catalog.Models;

public sealed class ProductVariant
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string OptionName { get; private set; } = string.Empty;

    public decimal ListPrice { get; private set; }
}
