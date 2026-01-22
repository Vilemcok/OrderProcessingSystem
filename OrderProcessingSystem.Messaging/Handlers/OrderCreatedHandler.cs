using Microsoft.EntityFrameworkCore;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.Handlers;

/// <summary>
/// Handles OrderCreated events published when new orders are created.
/// Updates order status to Processing and simulates payment processing.
/// 50% chance of success → publish OrderCompleted
/// 50% chance of failure → leave in Processing (will be expired by background job)
/// </summary>
public class OrderCreatedHandler : IHandleMessages<OrderCreated>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(
        AppDbContext dbContext,
        ILogger<OrderCreatedHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderCreated message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "Handling OrderCreated event for OrderId: {OrderId}",
            message.OrderId);

        // 1. Find order and update status: Pending → Processing
        var order = await _dbContext.Orders.FindAsync(message.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", message.OrderId);
            return;
        }

        order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Order {OrderId} status updated to Processing",
            message.OrderId);

        // 2. Simulate payment processing (5 seconds delay)
        _logger.LogInformation(
            "Simulating payment processing for Order {OrderId}...",
            message.OrderId);
        await Task.Delay(5000);

        // 3. Randomly decide outcome (50/50)
        var random = new Random();
        var isSuccess = random.Next(0, 2) == 1; // Returns 0 or 1

        if (isSuccess)
        {
            // 50% chance: Processing → Completed
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Publish OrderCompleted event
            await context.Publish(new OrderCompleted { OrderId = order.Id });

            _logger.LogInformation(
                "Order {OrderId} completed successfully and OrderCompleted event published",
                message.OrderId);
        }
        else
        {
            // 50% chance: Leave in Processing state
            // Background job will expire this order after 10 minutes
            _logger.LogInformation(
                "Order {OrderId} payment simulation failed, left in Processing state",
                message.OrderId);
        }
    }
}
