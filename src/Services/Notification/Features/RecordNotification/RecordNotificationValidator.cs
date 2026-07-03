using FluentValidation;

namespace ECommerce.Notification.Features.RecordNotification;

public sealed class RecordNotificationValidator : AbstractValidator<RecordNotificationCommand>
{
    public RecordNotificationValidator()
    {
        RuleFor(command => command.Type).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Recipient).NotEmpty().MaximumLength(320);
        RuleFor(command => command.Message).NotEmpty().MaximumLength(2000);
    }
}
