using ECommerce.Catalog.Data;
using ECommerce.Catalog.Models;
using ECommerce.Contracts;
using ECommerce.Contracts.Catalog;
using ECommerce.ServiceDefaults;
using ECommerce.ServiceDefaults.Messaging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Features.CreateProduct;

public sealed class CreateProductHandler(CatalogDbContext dbContext)
{
    public async Task<IResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        return (await Execute(command, cancellationToken)).ToHttpResult();
    }

    public async Task<OperationResult<CreateProductResponse>> Execute(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var validation = await new CreateProductValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<CreateProductResponse>.Validation(validation.ToDictionary());
        }

        var sku = command.Sku.Trim().ToUpperInvariant();
        if (await dbContext.Products.AnyAsync(product => product.Sku == sku, cancellationToken))
        {
            return OperationResult<CreateProductResponse>.Conflict("Product SKU already exists.");
        }

        var product = new Product(command.Name, sku, command.ListPrice, command.CategoryId, command.BrandId, command.Description);
        dbContext.Products.Add(product);
        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            KafkaTopics.ProductCreated,
            new ProductCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                product.Id,
                product.Sku,
                product.Name,
                product.ListPrice)));
        await dbContext.SaveChangesAsync(cancellationToken);

        return OperationResult<CreateProductResponse>.Created(
            $"/api/catalog/products/{product.Id}",
            new CreateProductResponse(product.Id, product.Sku));
    }
}
