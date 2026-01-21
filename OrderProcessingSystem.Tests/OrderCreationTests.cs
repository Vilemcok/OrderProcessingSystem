using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using Xunit;

namespace OrderProcessingSystem.Tests;

public class OrderCreationTests : IntegrationTestBase
{
    public OrderCreationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateOrder_WithSufficientStock_ReturnsCreated()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var productId = await GetTestProductIdAsync(100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = productId, Quantity = 5 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderResponse = await DeserializeResponse<OrderResponse>(response);
        orderResponse.Should().NotBeNull();
        orderResponse!.Id.Should().NotBeEmpty();
        orderResponse.Items.Should().HaveCount(1);
        orderResponse.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var productId = await GetLowStockProductIdAsync();
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = productId, Quantity = 100 } // More than available
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("stock");
    }

    [Fact]
    public async Task CreateOrder_ReducesStockQuantity()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var productId = await GetTestProductIdAsync(100);

        // Get initial stock
        int initialStock;
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await dbContext.Products.FindAsync(productId);
            initialStock = product!.Stock;
        }

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = productId, Quantity = 10 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify stock was reduced
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await dbContext.Products.FindAsync(productId);
            product!.Stock.Should().Be(initialStock - 10);
        }
    }

    [Fact]
    public async Task CreateOrder_WithMultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var products = dbContext.Products.Take(2).ToList();

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = products[0].Id, Quantity = 2 },
                new OrderItemRequest { ProductId = products[1].Id, Quantity = 3 }
            }
        };

        var expectedTotal = (products[0].Price * 2) + (products[1].Price * 3);

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderResponse = await DeserializeResponse<OrderResponse>(response);
        orderResponse.Should().NotBeNull();
        orderResponse!.Total.Should().Be(expectedTotal);
        orderResponse.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        ClearAuthToken();

        var productId = await GetTestProductIdAsync();
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
