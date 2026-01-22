using Microsoft.EntityFrameworkCore;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Repositories.Interfaces;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMessageSession _messageSession;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext context,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IMessageSession messageSession,
        ILogger<OrderService> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _messageSession = messageSession;
        _logger = logger;
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return null;

        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToResponse);
    }

    public async Task<IEnumerable<OrderResponse>> GetByUserIdAsync(Guid userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse?> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        // Start transaction
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Validate stock availability for all items
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in request.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product {item.ProductId} not found");

                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}");
            }

            // 2. Create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 3. Create order items and calculate total
            decimal total = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price
                };

                total += orderItem.Price * orderItem.Quantity;
                orderItems.Add(orderItem);

                // 4. Decrease stock
                product.Stock -= item.Quantity;
            }

            order.Total = total;
            order.OrderItems = orderItems;

            // 5. Save to database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // 6. Publish OrderCreated event AFTER successful commit
            await _messageSession.Publish(new OrderCreated { OrderId = order.Id });

            _logger.LogInformation(
                "Order {OrderId} created and OrderCreated event published",
                order.Id);

            // 7. Return response immediately (NO waiting for event processing)
            return new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                Total = order.Total,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = orderItems.Select(oi => new OrderItemResponse
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return false;

        await _orderRepository.DeleteAsync(id);
        return true;
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemResponse
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList()
        };
    }
}
