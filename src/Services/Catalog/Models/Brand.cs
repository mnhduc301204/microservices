namespace ECommerce.Catalog.Models;

public sealed class Brand
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;
}
