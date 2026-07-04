using ECommerce.Notification.Data;
using ECommerce.Notification.Features.RecordNotification;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Infrastructure.Tests;

public sealed class NotificationHandlerTests
{
    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task RecordNotification_WhenInputIsInvalid_DoesNotPersistNotification(RecordNotificationCommand command)
    {
        await using var dbContext = CreateDbContext();

        var result = await new RecordNotificationHandler(dbContext).Handle(command, CancellationToken.None);

        result.GetType().Name.Should().Contain("Problem");
        (await dbContext.Notifications.CountAsync()).Should().Be(0);
        (await dbContext.ProcessedMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RecordNotification_WhenSameSourceEventIsRetried_RecordsOnce()
    {
        await using var dbContext = CreateDbContext();
        var sourceEventId = Guid.NewGuid();
        var handler = new RecordNotificationHandler(dbContext);
        var command = new RecordNotificationCommand(
            "OrderCreated",
            "buyer@example.com",
            "Order was created.",
            sourceEventId);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        (await dbContext.Notifications.CountAsync()).Should().Be(1);
        (await dbContext.ProcessedMessages.CountAsync()).Should().Be(1);
    }

    public static TheoryData<RecordNotificationCommand> InvalidCommands()
    {
        var data = new TheoryData<RecordNotificationCommand>();
        data.Add(new RecordNotificationCommand("", "buyer@example.com", "message", null));
        data.Add(new RecordNotificationCommand(" ", "buyer@example.com", "message", null));
        data.Add(new RecordNotificationCommand(new string('T', 121), "buyer@example.com", "message", null));
        data.Add(new RecordNotificationCommand("OrderCreated", "", "message", null));
        data.Add(new RecordNotificationCommand("OrderCreated", " ", "message", null));
        data.Add(new RecordNotificationCommand("OrderCreated", new string('R', 321), "message", null));
        data.Add(new RecordNotificationCommand("OrderCreated", "buyer@example.com", "", null));
        data.Add(new RecordNotificationCommand("OrderCreated", "buyer@example.com", " ", null));
        data.Add(new RecordNotificationCommand("OrderCreated", "buyer@example.com", new string('M', 2001), null));
        return data;
    }

    private static NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }
}
