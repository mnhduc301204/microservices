using ECommerce.Catalog.Data;
using ECommerce.Catalog.Extensions;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCatalogData();
builder.AddCatalogMessaging();

var app = builder.Build();

await app.ApplyMigrations<CatalogDbContext>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapCatalogEndpoints();

app.Run();

public partial class Program;
