using ECommerce.Catalog.Models;
using ECommerce.ServiceDefaults.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(product => product.Id);
            builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
            builder.Property(product => product.Sku).HasMaxLength(64).IsRequired();
            builder.Property(product => product.Description).HasMaxLength(2000);
            builder.Property(product => product.ListPrice).HasPrecision(18, 2);
            builder.HasIndex(product => product.Sku).IsUnique();
            builder.HasMany(product => product.Variants).WithOne().HasForeignKey(variant => variant.ProductId);
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Brand>(builder =>
        {
            builder.HasKey(brand => brand.Id);
            builder.Property(brand => brand.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<ProductVariant>(builder =>
        {
            builder.HasKey(variant => variant.Id);
            builder.Property(variant => variant.Sku).HasMaxLength(64).IsRequired();
            builder.Property(variant => variant.OptionName).HasMaxLength(120).IsRequired();
            builder.Property(variant => variant.ListPrice).HasPrecision(18, 2);
            builder.HasIndex(variant => variant.Sku).IsUnique();
        });

        modelBuilder.AddInboxOutboxEntities();
    }
}
