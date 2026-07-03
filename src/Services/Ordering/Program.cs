using ECommerce.Ordering.Data;
using ECommerce.Ordering.Extensions;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderingData();
builder.AddOrderingMessaging();

var app = builder.Build();

await app.ApplyMigrations<OrderingDbContext>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapOrderingEndpoints();

app.Run();

public partial class Program;
