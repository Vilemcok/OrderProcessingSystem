using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using Xunit;

namespace OrderProcessingSystem.Tests;

public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await DeserializeResponse<LoginResponse>(response);
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrWhiteSpace();
        loginResponse.User.Should().NotBeNull();
        loginResponse.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "TestPassword123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestDataAsync();
        var loginRequest = new LoginRequest
        {
            Email = "not-an-email",
            Password = "TestPassword123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
