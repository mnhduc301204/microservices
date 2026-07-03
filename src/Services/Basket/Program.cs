using ECommerce.Basket.Data;
using ECommerce.Basket.Extensions;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddBasketData();
builder.AddBasketMessaging();

var app = builder.Build();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapBasketEndpoints();

app.Run();

public partial class Program;
