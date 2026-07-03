using ECommerce.Notification.Data;
using ECommerce.Notification.Extensions;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNotificationData();
builder.AddNotificationMessaging();

var app = builder.Build();

await app.ApplyMigrations<NotificationDbContext>();

app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.MapNotificationEndpoints();

app.Run();

public partial class Program;
