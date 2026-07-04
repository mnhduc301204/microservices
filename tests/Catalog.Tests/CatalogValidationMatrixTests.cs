using ECommerce.Catalog.Features.ChangeProductStatus;
using ECommerce.Catalog.Features.CreateProduct;
using ECommerce.Catalog.Features.UpdateProduct;
using ECommerce.Catalog.Models;
using FluentAssertions;

namespace ECommerce.Catalog.Tests;

public sealed class CatalogValidationMatrixTests
{
    [Theory]
    [MemberData(nameof(InvalidCreateProductCommands))]
    public void CreateProductValidator_InvalidInputs_FailValidation(CreateProductCommand command)
    {
        new CreateProductValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ValidCreateProductCommands))]
    public void CreateProductValidator_ValidInputs_PassValidation(CreateProductCommand command)
    {
        new CreateProductValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidUpdateProductCommands))]
    public void UpdateProductValidator_InvalidInputs_FailValidation(UpdateProductCommand command)
    {
        new UpdateProductValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(ValidUpdateProductCommands))]
    public void UpdateProductValidator_ValidInputs_PassValidation(UpdateProductCommand command)
    {
        new UpdateProductValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ChangeProductStatusCommands))]
    public void ChangeProductStatusValidator_ValidatesIdAndEnum(ChangeProductStatusCommand command, bool expected)
    {
        new ChangeProductStatusValidator().Validate(command).IsValid.Should().Be(expected);
    }

    public static TheoryData<CreateProductCommand> InvalidCreateProductCommands()
    {
        var data = new TheoryData<CreateProductCommand>();
        foreach (var name in new[] { "", " ", new string('N', 201) })
        {
            data.Add(ValidCreate() with { Name = name });
        }

        foreach (var sku in new[] { "", " ", new string('S', 65) })
        {
            data.Add(ValidCreate() with { Sku = sku });
        }

        foreach (var price in new[] { -0.01m, -1m, decimal.MinusOne })
        {
            data.Add(ValidCreate() with { ListPrice = price });
        }

        data.Add(ValidCreate() with { CategoryId = Guid.Empty });
        data.Add(ValidCreate() with { BrandId = Guid.Empty });
        data.Add(ValidCreate() with { Description = new string('D', 2001) });
        return data;
    }

    public static TheoryData<CreateProductCommand> ValidCreateProductCommands()
    {
        var data = new TheoryData<CreateProductCommand>();
        data.Add(ValidCreate());
        data.Add(ValidCreate() with { Description = null });
        data.Add(ValidCreate() with { ListPrice = 0m });
        data.Add(ValidCreate() with { Name = new string('N', 200) });
        data.Add(ValidCreate() with { Sku = new string('S', 64) });
        data.Add(ValidCreate() with { Description = new string('D', 2000) });
        return data;
    }

    public static TheoryData<UpdateProductCommand> InvalidUpdateProductCommands()
    {
        var data = new TheoryData<UpdateProductCommand> { ValidUpdate() with { Id = Guid.Empty } };
        foreach (var name in new[] { "", " ", new string('N', 201) })
        {
            data.Add(ValidUpdate() with { Name = name });
        }

        foreach (var price in new[] { -0.01m, -1m, decimal.MinusOne })
        {
            data.Add(ValidUpdate() with { ListPrice = price });
        }

        data.Add(ValidUpdate() with { CategoryId = Guid.Empty });
        data.Add(ValidUpdate() with { BrandId = Guid.Empty });
        data.Add(ValidUpdate() with { Description = new string('D', 2001) });
        return data;
    }

    public static TheoryData<UpdateProductCommand> ValidUpdateProductCommands()
    {
        var data = new TheoryData<UpdateProductCommand>();
        data.Add(ValidUpdate());
        data.Add(ValidUpdate() with { Description = null });
        data.Add(ValidUpdate() with { ListPrice = 0m });
        data.Add(ValidUpdate() with { Name = new string('N', 200) });
        data.Add(ValidUpdate() with { Description = new string('D', 2000) });
        return data;
    }

    public static TheoryData<ChangeProductStatusCommand, bool> ChangeProductStatusCommands()
    {
        var data = new TheoryData<ChangeProductStatusCommand, bool>();
        data.Add(new ChangeProductStatusCommand(Guid.NewGuid(), ProductStatus.Draft), true);
        data.Add(new ChangeProductStatusCommand(Guid.NewGuid(), ProductStatus.Active), true);
        data.Add(new ChangeProductStatusCommand(Guid.NewGuid(), ProductStatus.Archived), true);
        data.Add(new ChangeProductStatusCommand(Guid.Empty, ProductStatus.Active), false);
        data.Add(new ChangeProductStatusCommand(Guid.NewGuid(), (ProductStatus)999), false);
        data.Add(new ChangeProductStatusCommand(Guid.Empty, (ProductStatus)999), false);
        return data;
    }

    private static CreateProductCommand ValidCreate() =>
        new("Bottle", "SKU-1", 10m, Guid.NewGuid(), Guid.NewGuid(), "Reusable bottle");

    private static UpdateProductCommand ValidUpdate() =>
        new(Guid.NewGuid(), "Bottle", 10m, Guid.NewGuid(), Guid.NewGuid(), "Reusable bottle");
}
