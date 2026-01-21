using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Tests;

public class IntegrationTestBase : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly JsonSerializerOptions JsonOptions;

    public IntegrationTestBase(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task<string> GetAuthTokenAsync(string email = "test@example.com", string password = "TestPassword123")
    {
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse!.Token;
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected async Task SeedTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear existing data
        dbContext.OrderItems.RemoveRange(dbContext.OrderItems);
        dbContext.Orders.RemoveRange(dbContext.Orders);
        dbContext.Products.RemoveRange(dbContext.Products);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        // Add test user
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "TestPassword123", // Plain text as per requirements
            Name = "Test User"
        };
        dbContext.Users.Add(testUser);

        // Add test products
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product 1",
            Price = 10.99m,
            Stock = 100,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product 2",
            Price = 25.50m,
            Stock = 5,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Products.AddRange(product1, product2);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<Guid> GetTestProductIdAsync(int stockQuantity = 100)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Stock >= stockQuantity);
        return product?.Id ?? Guid.Empty;
    }

    protected async Task<Guid> GetLowStockProductIdAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Stock < 10);
        return product?.Id ?? Guid.Empty;
    }
}
