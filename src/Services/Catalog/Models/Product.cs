namespace ECommerce.Catalog.Models;

public sealed class Product
{
    private readonly List<ProductVariant> _variants = [];

    private Product()
    {
    }

    public Product(string name, string sku, decimal listPrice, Guid categoryId, Guid brandId, string? description)
    {
        Rename(name);
        SetSku(sku);
        ChangePrice(listPrice);
        CategoryId = categoryId;
        BrandId = brandId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = ProductStatus.Draft;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal ListPrice { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid BrandId { get; private set; }

    public ProductStatus Status { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants;

    public void Update(string name, decimal listPrice, Guid categoryId, Guid brandId, string? description)
    {
        Rename(name);
        ChangePrice(listPrice);
        CategoryId = categoryId;
        BrandId = brandId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void ChangeStatus(ProductStatus status)
    {
        Status = status;
    }

    private void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("Product SKU is required.", nameof(sku));
        }

        Sku = sku.Trim().ToUpperInvariant();
    }

    private void ChangePrice(decimal listPrice)
    {
        if (listPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(listPrice), "List price cannot be negative.");
        }

        ListPrice = listPrice;
    }
}

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2,
}
