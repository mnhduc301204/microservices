using ECommerce.Notification.Data;
using ECommerce.Notification.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Notification.Features.RecordNotification;

public sealed class RecordNotificationHandler(NotificationDbContext dbContext)
{
    public async Task<IResult> Handle(RecordNotificationCommand command, CancellationToken cancellationToken)
    {
        var validation = await new RecordNotificationValidator().ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (command.SourceEventId.HasValue &&
            await dbContext.ProcessedMessages.AnyAsync(message => message.EventId == command.SourceEventId.Value, cancellationToken))
        {
            return Results.Accepted();
        }

        var notification = new NotificationRecord(command.Type, command.Recipient, command.Message, command.SourceEventId);
        dbContext.Notifications.Add(notification);
        if (command.SourceEventId.HasValue)
        {
            dbContext.ProcessedMessages.Add(new ProcessedMessage { EventId = command.SourceEventId.Value });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/notifications/{notification.Id}", new RecordNotificationResponse(notification.Id));
    }
}
