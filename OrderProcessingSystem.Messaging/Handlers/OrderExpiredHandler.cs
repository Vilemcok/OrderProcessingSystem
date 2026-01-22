using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.Handlers;

/// <summary>
/// Handles OrderExpired events.
/// Saves notification record to database (no email needed).
/// </summary>
public class OrderExpiredHandler : IHandleMessages<OrderExpired>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderExpiredHandler> _logger;

    public OrderExpiredHandler(
        AppDbContext dbContext,
        ILogger<OrderExpiredHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderExpired message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "Handling OrderExpired event for OrderId: {OrderId}",
            message.OrderId);

        // Save notification record to database
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Type = NotificationType.Expired,
            Message = $"Order {message.OrderId} has expired due to processing timeout.",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Notification saved to database for expired Order {OrderId}",
            message.OrderId);
    }
}
