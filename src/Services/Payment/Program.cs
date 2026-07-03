using ECommerce.Payment.Data;
using ECommerce.Payment.Extensions;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPaymentData();
builder.AddPaymentMessaging();

var app = builder.Build();

await app.ApplyMigrations<PaymentDbContext>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapPaymentEndpoints();

app.Run();

public partial class Program;
