using ECommerce.Inventory.Data;
using ECommerce.Inventory.Extensions;
using ECommerce.Inventory.BackgroundServices;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInventoryData();
builder.Services.AddHostedService<ReservationExpiryService>();
builder.AddInventoryMessaging();

var app = builder.Build();

await app.ApplyMigrations<InventoryDbContext>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapInventoryEndpoints();

app.Run();

public partial class Program;
