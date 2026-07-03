using ECommerce.Notification.Features.GetNotifications;
using ECommerce.Notification.Features.RecordNotification;

namespace ECommerce.Notification.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notification");

        group.MapRecordNotification();
        group.MapGetNotifications();

        return app;
    }
}
