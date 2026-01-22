using Microsoft.EntityFrameworkCore;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.BackgroundJobs;

/// <summary>
/// Background service that runs every 60 seconds to check for expired orders.
/// Finds orders with status=Processing and CreatedAt older than 10 minutes,
/// updates their status to Expired, and publishes OrderExpired events.
/// </summary>
public class OrderExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpirationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _expirationThreshold = TimeSpan.FromMinutes(10);

    public OrderExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OrderExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationBackgroundService started");

        // Use PeriodicTimer (modern approach for .NET)
        using var timer = new PeriodicTimer(_checkInterval);

        try
        {
            // Run immediately on startup, then every interval
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckForExpiredOrdersAsync(stoppingToken);

                // Wait for next tick
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderExpirationBackgroundService is stopping");
        }
    }

    private async Task CheckForExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking for expired orders...");

        try
        {
            // Create a new scope for scoped services (DbContext, IMessageSession)
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var messageSession = scope.ServiceProvider.GetRequiredService<IMessageSession>();

            // Calculate expiration cutoff time
            var expirationCutoff = DateTime.UtcNow - _expirationThreshold;

            // Find orders with status=Processing and created more than 10 minutes ago
            var expiredOrders = await dbContext.Orders
                .Where(o => o.Status == OrderStatus.Processing
                         && o.CreatedAt < expirationCutoff)
                .ToListAsync(cancellationToken);

            if (expiredOrders.Count == 0)
            {
                _logger.LogDebug("No expired orders found");
                return;
            }

            _logger.LogInformation(
                "Found {Count} expired orders to process",
                expiredOrders.Count);

            // Process each expired order
            foreach (var order in expiredOrders)
            {
                // Update status to Expired
                order.Status = OrderStatus.Expired;
                order.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Order {OrderId} expired (created at {CreatedAt})",
                    order.Id,
                    order.CreatedAt);

                // Publish OrderExpired event
                await messageSession.Publish(new OrderExpired { OrderId = order.Id });
            }

            // Save all changes in one transaction
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed {Count} expired orders",
                expiredOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while checking for expired orders");
            // Don't rethrow - let the timer continue
        }
    }
}
