using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.Handlers;

/// <summary>
/// Handles OrderCompleted events.
/// Logs fake email notification and saves notification record to database.
/// </summary>
public class OrderCompletedHandler : IHandleMessages<OrderCompleted>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCompletedHandler> _logger;

    public OrderCompletedHandler(
        AppDbContext dbContext,
        ILogger<OrderCompletedHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderCompleted message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "Handling OrderCompleted event for OrderId: {OrderId}",
            message.OrderId);

        // 1. Log fake email notification
        _logger.LogInformation(
            "📧 FAKE EMAIL: Order {OrderId} has been completed successfully!",
            message.OrderId);

        // 2. Save notification record to database
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Type = NotificationType.Completed,
            Message = $"Order {message.OrderId} has been completed successfully.",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Notification saved to database for Order {OrderId}",
            message.OrderId);
    }
}
