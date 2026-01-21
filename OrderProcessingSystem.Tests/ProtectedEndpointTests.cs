using System.Net;
using FluentAssertions;
using Xunit;

namespace OrderProcessingSystem.Tests;

public class ProtectedEndpointTests : IntegrationTestBase
{
    public ProtectedEndpointTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetOrders_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        ClearAuthToken();

        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        ClearAuthToken();

        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        SetAuthToken("invalid.token.here");

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
