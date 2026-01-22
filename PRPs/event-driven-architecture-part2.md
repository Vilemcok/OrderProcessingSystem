# PRP: Event-Driven Architecture with RabbitMQ and NServiceBus (Part 2)

## Feature Overview

Extend the existing OrderProcessingSystem with event-driven architecture using RabbitMQ and NServiceBus. This implementation adds asynchronous order processing, background job expiration handling, and notification tracking while maintaining **full backward compatibility** with Part 1.

## Critical Constraints

**DO NOT:**
- ❌ Refactor or split the existing OrderProcessingSystem.WebApi project
- ❌ Move existing controllers, EF Core code, or authentication logic
- ❌ Break or modify Part 1 functionality
- ❌ Invent features not listed in requirements
- ❌ Containerize the API in Docker Compose

**DO:**
- ✅ Add new projects instead of restructuring
- ✅ Keep backward compatibility with Part 1
- ✅ Follow all causal flows exactly as described
- ✅ Reuse existing PostgreSQL database
- ✅ Use Docker Compose ONLY for infrastructure (Postgres + RabbitMQ)

## Technology Stack (Fixed Decisions)

| Component | Technology | Version/Notes |
|-----------|-----------|---------------|
| **Messaging Broker** | RabbitMQ | Used ONLY as transport |
| **Messaging Framework** | NServiceBus | Event bus abstraction |
| **Transport** | NServiceBus.RabbitMQ | Version 10.1.7 |
| **Background Jobs** | ASP.NET Core BackgroundService | With PeriodicTimer |
| **Database** | PostgreSQL | Reuse existing DB |
| **ORM** | Entity Framework Core 10 | With migrations |
| **Testing** | xUnit + Testcontainers | PostgreSQL + RabbitMQ |
| **Worker SDK** | Microsoft.NET.Sdk.Worker | .NET 10.0 |

## Documentation References

### NServiceBus Core & RabbitMQ Transport
- **NServiceBus Documentation**: https://docs.particular.net/nservicebus/
- **Messages, Events, and Commands**: https://docs.particular.net/nservicebus/messaging/messages-events-commands
- **Publishing Events Tutorial**: https://docs.particular.net/tutorials/nservicebus-step-by-step/4-publishing-events/
- **Publish-Subscribe Pattern**: https://docs.particular.net/nservicebus/messaging/publish-subscribe/
- **Publishing from Web Applications**: https://docs.particular.net/nservicebus/hosting/publishing-from-web-applications
- **RabbitMQ Transport**: https://docs.particular.net/transports/rabbitmq/
- **Simple RabbitMQ Usage Sample**: https://docs.particular.net/samples/rabbitmq/simple/
- **NServiceBus.RabbitMQ NuGet**: https://www.nuget.org/packages/NServiceBus.RabbitMQ (v10.1.7)

### ASP.NET Core & Background Services
- **Background Tasks with Hosted Services**: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0
- **Worker Services in .NET**: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- **PeriodicTimer Pattern**: Modern approach for recurring tasks

### Testcontainers
- **Testcontainers for .NET**: https://dotnet.testcontainers.org/
- **RabbitMQ Module**: https://dotnet.testcontainers.org/modules/rabbitmq/
- **PostgreSQL Module**: https://dotnet.testcontainers.org/modules/postgres/
- **Testcontainers.RabbitMq NuGet**: https://www.nuget.org/packages/Testcontainers.RabbitMq (v4.10.0)

### Visual Studio Multi-Project Setup
- **Set Multiple Startup Projects**: https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects?view=visualstudio
- **Multi-Project Launch Profiles**: https://mzikmund.dev/blog/multi-project-launch-profiles-in-visual-studio

### Entity Framework Core & Docker
- **EF Core Migrations**: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
- **Docker Compose Documentation**: https://docs.docker.com/compose/
- **RabbitMQ Docker Image**: https://hub.docker.com/_/rabbitmq

## Existing Codebase Patterns to Follow

### 1. Current Project Structure (DO NOT MODIFY)

```
OrderProcessingSystem.WebApi/
├── Controllers/          # API Controllers (OrdersController.cs)
├── Models/
│   ├── Entities/        # Database entities (Order.cs, User.cs, Product.cs)
│   └── DTOs/            # Request/Response DTOs
├── Services/
│   ├── Interfaces/      # Service interfaces (IOrderService.cs)
│   └── Implementations/ # Service implementations (OrderService.cs)
├── Repositories/
│   ├── Interfaces/      # Repository interfaces
│   └── Implementations/ # Repository implementations
├── Data/                # AppDbContext.cs
├── Middleware/          # GlobalExceptionHandlerMiddleware.cs
└── Migrations/          # EF Core migrations
```

**Reference Files:**
- `OrderProcessingSystem.WebApi/Controllers/OrdersController.cs:44-64` - Controller pattern
- `OrderProcessingSystem.WebApi/Services/Implementations/OrderService.cs:47-137` - Transaction pattern
- `OrderProcessingSystem.WebApi/Data/AppDbContext.cs:1-136` - DbContext configuration

### 2. Order Entity and Status Enum

**Reference: `OrderProcessingSystem.WebApi/Models/Entities/Order.cs:5-31`**

```csharp
public enum OrderStatus
{
    Pending,      // ✅ Initial state when order is created via API
    Processing,   // ✅ After OrderCreated event handler starts processing
    Completed,    // ✅ After successful payment simulation (50% chance)
    Expired       // ✅ After background job finds order stuck in Processing
}

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

**CRITICAL:** The `OrderStatus` enum already exists. Do NOT recreate it.

### 3. Database Context Pattern

**Reference: `OrderProcessingSystem.WebApi/Data/AppDbContext.cs:6-16`**

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    // ✅ ADD NEW: public DbSet<Notification> Notifications { get; set; }
}
```

**Pattern for Notification Entity:**
- Create entity class in `OrderProcessingSystem.WebApi/Models/Entities/Notification.cs`
- Add DbSet to AppDbContext
- Configure in OnModelCreating
- Create EF Core migration

### 4. Transaction Pattern for Order Creation

**Reference: `OrderProcessingSystem.WebApi/Services/Implementations/OrderService.cs:47-137`**

```csharp
public async Task<OrderResponse?> CreateOrderAsync(Guid userId, CreateOrderRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Validate stock
        // 2. Create order with Status = Pending
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,  // ✅ CRITICAL: Set to Pending
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Create order items, calculate total
        // 4. Save to database
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 5. Commit transaction
        await transaction.CommitAsync();

        // ✅ EXTENSION POINT: Publish OrderCreated event HERE
        // await _messageSession.Publish(new OrderCreated { OrderId = order.Id });

        // 6. Return response immediately (NO waiting)
        return MapToResponse(order);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**CRITICAL FLOW:**
1. ✅ Begin transaction → Create order → Commit transaction
2. ✅ Publish event AFTER commit
3. ✅ Return response immediately (API does NOT wait for event processing)
4. ❌ NO payment simulation in API
5. ❌ NO status updates beyond 'Pending' in API

### 5. Dependency Injection Pattern

**Reference: `OrderProcessingSystem.WebApi/Program.cs:46-56`**

```csharp
// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register services
builder.Services.AddScoped<IOrderService, OrderService>();
// ... other services
```

**Extension for Part 2:**
- Inject `IMessageSession` into `OrderService` for publishing events
- Register NServiceBus message session in DI container

### 6. Integration Testing Pattern

**Reference: `OrderProcessingSystem.Tests/IntegrationTestBase.cs:13-120`**

```csharp
public class IntegrationTestBase : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebAppFactory Factory;

    protected async Task SeedTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Clear existing data
        dbContext.OrderItems.RemoveRange(dbContext.OrderItems);
        dbContext.Orders.RemoveRange(dbContext.Orders);
        await dbContext.SaveChangesAsync();

        // Seed test data
    }
}
```

**Reference: `OrderProcessingSystem.Tests/IntegrationTestWebAppFactory.cs:10-56`**

```csharp
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("orderprocessing_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
```

**Pattern for Messaging Tests:**
- Create similar factory for messaging endpoint
- Add `Testcontainers.RabbitMq` alongside PostgreSQL
- Test event handlers with real containers

## Implementation Blueprint

### PHASE 1: Add New Projects to Solution

#### Task 1.1: Create OrderProcessingSystem.Messaging Project

```bash
# Navigate to solution root
cd C:\Workspace_Vipo\OrderProcessingSystem

# Create worker service project
dotnet new worker -n OrderProcessingSystem.Messaging

# Move to correct location if needed
# Ensure it's at same level as OrderProcessingSystem.WebApi
```

**Edit `OrderProcessingSystem.Messaging/OrderProcessingSystem.Messaging.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>messaging-secrets-id</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <!-- NServiceBus Core -->
    <PackageReference Include="NServiceBus" Version="9.2.4" />
    <PackageReference Include="NServiceBus.RabbitMQ" Version="10.1.7" />
    <PackageReference Include="NServiceBus.Extensions.Hosting" Version="3.0.0" />

    <!-- Database -->
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- Worker Service -->
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference existing WebApi project for entities and DbContext -->
    <ProjectReference Include="..\OrderProcessingSystem.WebApi\OrderProcessingSystem.WebApi.csproj" />
  </ItemGroup>
</Project>
```

#### Task 1.2: Create OrderProcessingSystem.Messaging.Tests Project

```bash
dotnet new xunit -n OrderProcessingSystem.Messaging.Tests
```

**Edit `OrderProcessingSystem.Messaging.Tests/OrderProcessingSystem.Messaging.Tests.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="FluentAssertions" Version="8.8.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />

    <!-- Testcontainers -->
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.10.0" />
    <PackageReference Include="Testcontainers.RabbitMq" Version="4.10.0" />

    <!-- NServiceBus Testing -->
    <PackageReference Include="NServiceBus.Testing" Version="9.2.0" />

    <!-- Database -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\OrderProcessingSystem.Messaging\OrderProcessingSystem.Messaging.csproj" />
    <ProjectReference Include="..\OrderProcessingSystem.WebApi\OrderProcessingSystem.WebApi.csproj" />
  </ItemGroup>
</Project>
```

#### Task 1.3: Add Projects to Solution

```bash
# Add new projects to solution
dotnet sln OrderProcessingSystem.sln add OrderProcessingSystem.Messaging/OrderProcessingSystem.Messaging.csproj
dotnet sln OrderProcessingSystem.sln add OrderProcessingSystem.Messaging.Tests/OrderProcessingSystem.Messaging.Tests.csproj

# Verify solution structure
dotnet sln OrderProcessingSystem.sln list
```

**Expected output:**
```
OrderProcessingSystem.WebApi\OrderProcessingSystem.WebApi.csproj
OrderProcessingSystem.Tests\OrderProcessingSystem.Tests.csproj
OrderProcessingSystem.Messaging\OrderProcessingSystem.Messaging.csproj
OrderProcessingSystem.Messaging.Tests\OrderProcessingSystem.Messaging.Tests.csproj
```

### PHASE 2: Define Integration Events

#### Task 2.1: Create Events in Messaging Project

**Create folder structure:**
```
OrderProcessingSystem.Messaging/
├── Events/
│   ├── OrderCreated.cs
│   ├── OrderCompleted.cs
│   └── OrderExpired.cs
```

**Create: `OrderProcessingSystem.Messaging/Events/OrderCreated.cs`**

```csharp
using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when a new order is created via the API.
/// Published after order is saved with status = Pending.
/// </summary>
public class OrderCreated : IEvent
{
    public Guid OrderId { get; set; }
}
```

**Create: `OrderProcessingSystem.Messaging/Events/OrderCompleted.cs`**

```csharp
using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when order processing completes successfully.
/// Published from OrderCreatedHandler after payment simulation succeeds (50% chance).
/// </summary>
public class OrderCompleted : IEvent
{
    public Guid OrderId { get; set; }
}
```

**Create: `OrderProcessingSystem.Messaging/Events/OrderExpired.cs`**

```csharp
using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

/// <summary>
/// Event published when an order expires due to being stuck in Processing state.
/// Published from OrderExpirationBackgroundService for orders older than 10 minutes.
/// </summary>
public class OrderExpired : IEvent
{
    public Guid OrderId { get; set; }
}
```

**Event Design Principles:**
- ✅ Events implement `NServiceBus.IEvent`
- ✅ Events contain ONLY necessary data (OrderId)
- ✅ Events are named in past tense (OrderCreated, not CreateOrder)
- ✅ Events represent something that has already happened
- ✅ Events are immutable (no setters needed, but use auto-properties for serialization)

### PHASE 3: Add Notification Entity to Database

#### Task 3.1: Create Notification Entity

**Create: `OrderProcessingSystem.WebApi/Models/Entities/Notification.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.Entities;

public enum NotificationType
{
    Completed,
    Expired
}

public class Notification
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Order Order { get; set; } = null!;
}
```

#### Task 3.2: Update AppDbContext

**Edit: `OrderProcessingSystem.WebApi/Data/AppDbContext.cs`**

Add DbSet property:
```csharp
public DbSet<Notification> Notifications { get; set; }
```

Add configuration in `OnModelCreating`:
```csharp
// Notification configuration
modelBuilder.Entity<Notification>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
    entity.Property(e => e.Type).HasConversion<string>();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

    entity.HasOne(e => e.Order)
        .WithMany()
        .HasForeignKey(e => e.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

#### Task 3.3: Create and Apply Migration

```bash
# Navigate to WebApi project
cd OrderProcessingSystem.WebApi

# Create migration
dotnet ef migrations add AddNotificationTable

# Apply migration
dotnet ef database update
```

**Verify migration created:**
- Check `OrderProcessingSystem.WebApi/Migrations/` for new migration file
- Verify migration includes Notifications table creation

### PHASE 4: Configure NServiceBus in Messaging Worker

#### Task 4.1: Configure Program.cs in Messaging Project

**Edit: `OrderProcessingSystem.Messaging/Program.cs`**

Delete the default Worker.cs and replace Program.cs with:

```csharp
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.BackgroundJobs;

var builder = Host.CreateApplicationBuilder(args);

// Configure NServiceBus
builder.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("OrderProcessingSystem.Messaging");

    // Configure RabbitMQ transport
    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
    var connectionString = builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "host=localhost;username=guest;password=guest";
    transport.ConnectionString(connectionString);
    transport.UseConventionalRoutingTopology(QueueType.Quorum);

    // Use Learning persistence (simple file-based persistence for development)
    // For production, consider NServiceBus.Persistence.Sql with PostgreSQL
    endpointConfiguration.UsePersistence<LearningPersistence>();

    // Enable installers (auto-creates queues in development)
    endpointConfiguration.EnableInstallers();

    // Configure serialization
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    // Configure recoverability (retry policy)
    var recoverability = endpointConfiguration.Recoverability();
    recoverability.Immediate(immediate => immediate.NumberOfRetries(3));
    recoverability.Delayed(delayed => delayed.NumberOfRetries(2));

    return endpointConfiguration;
});

// Add DbContext with same connection string as WebApi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register background services
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

var host = builder.Build();
await host.RunAsync();
```

**Create: `OrderProcessingSystem.Messaging/appsettings.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=OrderProcessingDB;Username=orderuser;Password=orderpass123",
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "NServiceBus": "Information"
    }
  }
}
```

**Critical Configuration Notes:**
- ✅ Endpoint name: "OrderProcessingSystem.Messaging"
- ✅ RabbitMQ: ConventionalRoutingTopology with QueueType.Quorum
- ✅ LearningPersistence: Simple for development (stores saga data in files)
- ✅ EnableInstallers(): Auto-creates RabbitMQ queues
- ✅ SystemJsonSerializer: Uses System.Text.Json
- ✅ Recoverability: 3 immediate retries, 2 delayed retries

### PHASE 5: Implement Event Handlers

#### Task 5.1: OrderCreatedHandler

**Create: `OrderProcessingSystem.Messaging/Handlers/OrderCreatedHandler.cs`**

```csharp
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
```

**Key Patterns:**
- ✅ Implements `IHandleMessages<OrderCreated>`
- ✅ Injects `AppDbContext` (NServiceBus creates scope automatically)
- ✅ Uses `IMessageHandlerContext.Publish()` to publish events
- ✅ All operations are async/await
- ✅ Comprehensive logging for observability
- ✅ Random.Next(0, 2) returns 0 or 1 (50/50 split)

#### Task 5.2: OrderCompletedHandler

**Create: `OrderProcessingSystem.Messaging/Handlers/OrderCompletedHandler.cs`**

```csharp
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
```

#### Task 5.3: OrderExpiredHandler

**Create: `OrderProcessingSystem.Messaging/Handlers/OrderExpiredHandler.cs`**

```csharp
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
```

### PHASE 6: Implement Background Job for Order Expiration

#### Task 6.1: Create OrderExpirationBackgroundService

**Create: `OrderProcessingSystem.Messaging/BackgroundJobs/OrderExpirationBackgroundService.cs`**

```csharp
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
```

**Key Patterns:**
- ✅ Inherits from `BackgroundService`
- ✅ Uses `PeriodicTimer` (modern .NET approach, non-blocking)
- ✅ Runs every 60 seconds
- ✅ Creates scope for scoped services (DbContext, IMessageSession)
- ✅ Finds orders with `Status == Processing` and `CreatedAt < (UtcNow - 10 minutes)`
- ✅ Updates status to `Expired` and publishes `OrderExpired` event
- ✅ Handles exceptions without stopping the timer
- ✅ Comprehensive logging

### PHASE 7: Integrate Event Publishing into WebApi

#### Task 7.1: Modify OrderService to Publish OrderCreated Event

**Edit: `OrderProcessingSystem.WebApi/Services/Implementations/OrderService.cs`**

Add constructor parameter for IMessageSession:

```csharp
using NServiceBus;
using OrderProcessingSystem.Messaging.Events;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMessageSession _messageSession; // ADD THIS

    public OrderService(
        AppDbContext context,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IMessageSession messageSession) // ADD THIS
    {
        _context = context;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _messageSession = messageSession; // ADD THIS
    }

    public async Task<OrderResponse?> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ... existing code for validation, order creation, stock update ...

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // ✅ NEW: Publish OrderCreated event AFTER successful commit
            await _messageSession.Publish(new OrderCreated { OrderId = order.Id });

            _logger.LogInformation(
                "Order {OrderId} created and OrderCreated event published",
                order.Id);

            // Return response immediately (NO waiting for event processing)
            return MapToResponse(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ... rest of existing methods unchanged ...
}
```

**Add using statement at top of file:**
```csharp
using NServiceBus;
using OrderProcessingSystem.Messaging.Events;
```

#### Task 7.2: Configure NServiceBus in WebApi Program.cs

**Edit: `OrderProcessingSystem.WebApi/Program.cs`**

Add NServiceBus configuration BEFORE `var app = builder.Build();`:

```csharp
// Add NServiceBus for event publishing
builder.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("OrderProcessingSystem.WebApi");

    // Configure RabbitMQ transport
    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
    var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
        ?? "host=localhost;username=guest;password=guest";
    transport.ConnectionString(rabbitMqConnectionString);
    transport.UseConventionalRoutingTopology(QueueType.Quorum);

    // Use Learning persistence for send-only endpoint
    endpointConfiguration.UsePersistence<LearningPersistence>();

    // Enable installers
    endpointConfiguration.EnableInstallers();

    // Configure as send-only endpoint (publishes events but doesn't process them)
    endpointConfiguration.SendOnly();

    return endpointConfiguration;
});
```

**Add to appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=OrderProcessingDB;Username=orderuser;Password=orderpass123",
    "RabbitMQ": "host=localhost;username=guest;password=guest"
  }
}
```

**Add NuGet packages to OrderProcessingSystem.WebApi.csproj:**
```xml
<PackageReference Include="NServiceBus" Version="9.2.4" />
<PackageReference Include="NServiceBus.RabbitMQ" Version="10.1.7" />
<PackageReference Include="NServiceBus.Extensions.Hosting" Version="3.0.0" />
```

**CRITICAL NOTES:**
- ✅ WebApi uses `SendOnly()` - it publishes events but does NOT handle them
- ✅ Messaging worker handles all events
- ✅ Both use same RabbitMQ connection string
- ✅ Both use ConventionalRoutingTopology with QueueType.Quorum

### PHASE 8: Update Docker Compose for RabbitMQ

#### Task 8.1: Add RabbitMQ Service to docker-compose.yml

**Edit: `docker-compose.yml`**

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: orderprocessing_db
    environment:
      POSTGRES_USER: orderuser
      POSTGRES_PASSWORD: orderpass123
      POSTGRES_DB: OrderProcessingDB
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: orderprocessing_rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"    # AMQP port
      - "15672:15672"  # Management UI port
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  postgres_data:
  rabbitmq_data:
```

**RabbitMQ Management UI:**
- URL: http://localhost:15672
- Username: guest
- Password: guest

### PHASE 9: Configure Visual Studio Multi-Project Startup

#### Task 9.1: Create Multi-Project Launch Profile

Visual Studio 2022+ supports two approaches:

**Option A: Use Solution Properties (Manual)**
1. Right-click solution in Solution Explorer
2. Select "Configure Startup Projects"
3. Choose "Multiple startup projects"
4. Set both projects to "Start":
   - OrderProcessingSystem.WebApi → Start
   - OrderProcessingSystem.Messaging → Start

**Option B: Create .slnLaunch file (Recommended for sharing)**

**Create: `OrderProcessingSystem.slnLaunch`** (next to .sln file)

```json
{
  "version": 1,
  "profiles": {
    "OrderProcessingSystem": {
      "projects": {
        "OrderProcessingSystem.WebApi\\OrderProcessingSystem.WebApi.csproj": {
          "commandName": "Project",
          "launchBrowser": true,
          "launchUrl": "swagger"
        },
        "OrderProcessingSystem.Messaging\\OrderProcessingSystem.Messaging.csproj": {
          "commandName": "Project"
        }
      }
    }
  }
}
```

**When user presses F5:**
- ✅ WebApi starts with Swagger UI opening
- ✅ Messaging worker starts in background
- ✅ Both connect to RabbitMQ and PostgreSQL

### PHASE 10: Create Integration Tests

#### Task 10.1: Create MessagingIntegrationTestFactory

**Create: `OrderProcessingSystem.Messaging.Tests/MessagingIntegrationTestFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderProcessingSystem.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OrderProcessingSystem.Messaging.Tests;

public class MessagingIntegrationTestFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("orderprocessing_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private IHost? _host;

    public async Task InitializeAsync()
    {
        // Start containers
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        // Build and start messaging host
        var builder = Host.CreateApplicationBuilder();

        // Configure test services
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_dbContainer.GetConnectionString()));

        // Configure NServiceBus with test containers
        builder.UseNServiceBus(context =>
        {
            var endpointConfiguration = new EndpointConfiguration("OrderProcessingSystem.Messaging.Tests");

            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.ConnectionString(_rabbitMqContainer.GetConnectionString());
            transport.UseConventionalRoutingTopology(QueueType.Quorum);

            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.UseSerialization<SystemJsonSerializer>();

            return endpointConfiguration;
        });

        _host = builder.Build();

        // Apply migrations
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        // Start the host
        await _host.StartAsync();
    }

    public IServiceProvider Services => _host!.Services;

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await _rabbitMqContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
```

#### Task 10.2: Create OrderCreatedHandlerTests

**Create: `OrderProcessingSystem.Messaging.Tests/OrderCreatedHandlerTests.cs`**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.Tests;

public class OrderCreatedHandlerTests : IClassFixture<MessagingIntegrationTestFactory>
{
    private readonly MessagingIntegrationTestFactory _factory;

    public OrderCreatedHandlerTests(MessagingIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_OrderCreatedEvent_UpdatesOrderStatusToProcessing()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageSession = scope.ServiceProvider.GetRequiredService<IMessageSession>();

        // Create test order with Pending status
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Total = 100.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        await messageSession.Publish(new OrderCreated { OrderId = order.Id });

        // Wait for handler to process (5 seconds payment simulation + buffer)
        await Task.Delay(7000);

        // Assert
        var updatedOrder = await dbContext.Orders.FindAsync(order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().BeOneOf(OrderStatus.Processing, OrderStatus.Completed);
    }

    [Fact]
    public async Task Handle_OrderCreatedEvent_PublishesOrderCompletedOnSuccess()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageSession = scope.ServiceProvider.GetRequiredService<IMessageSession>();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Total = 50.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        // Act
        await messageSession.Publish(new OrderCreated { OrderId = order.Id });

        // Wait for processing
        await Task.Delay(7000);

        // Assert
        var updatedOrder = await dbContext.Orders.FindAsync(order.Id);
        if (updatedOrder!.Status == OrderStatus.Completed)
        {
            // If completed, notification should exist
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.OrderId == order.Id);

            notification.Should().NotBeNull();
            notification!.Type.Should().Be(NotificationType.Completed);
        }
    }
}
```

#### Task 10.3: Create OrderExpirationBackgroundServiceTests

**Create: `OrderProcessingSystem.Messaging.Tests/OrderExpirationBackgroundServiceTests.cs`**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Messaging.Tests;

public class OrderExpirationBackgroundServiceTests : IClassFixture<MessagingIntegrationTestFactory>
{
    private readonly MessagingIntegrationTestFactory _factory;

    public OrderExpirationBackgroundServiceTests(MessagingIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BackgroundService_ExpiresOldProcessingOrders()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create order with Processing status from 11 minutes ago
        var oldOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Total = 100.00m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-11),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-11)
        };

        dbContext.Orders.Add(oldOrder);
        await dbContext.SaveChangesAsync();

        // Act - Wait for background service to run (60 second interval + buffer)
        await Task.Delay(65000);

        // Assert
        var expiredOrder = await dbContext.Orders.FindAsync(oldOrder.Id);
        expiredOrder.Should().NotBeNull();
        expiredOrder!.Status.Should().Be(OrderStatus.Expired);

        // Check notification was created
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.OrderId == oldOrder.Id);

        notification.Should().NotBeNull();
        notification!.Type.Should().Be(NotificationType.Expired);
    }

    [Fact]
    public async Task BackgroundService_DoesNotExpireRecentProcessingOrders()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create order with Processing status from 5 minutes ago (not old enough)
        var recentOrder = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Total = 50.00m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        dbContext.Orders.Add(recentOrder);
        await dbContext.SaveChangesAsync();

        // Act - Wait for background service
        await Task.Delay(65000);

        // Assert
        var order = await dbContext.Orders.FindAsync(recentOrder.Id);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Processing); // Still processing
    }
}
```

### PHASE 11: Update README Documentation

#### Task 11.1: Update README.md with Part 2 Information

**Edit: `README.md`**

Add new sections after existing content:

```markdown
## Part 2: Event-Driven Architecture

### Architecture Overview

Part 2 extends the system with asynchronous order processing using event-driven architecture:

**Components:**
- **OrderProcessingSystem.WebApi** - REST API (publishes OrderCreated events)
- **OrderProcessingSystem.Messaging** - Worker service (handles events, runs background jobs)
- **RabbitMQ** - Message broker for event transport
- **NServiceBus** - Messaging framework abstraction

**Event Flow:**
1. User creates order via API → Order saved with status=Pending
2. API publishes OrderCreated event → Returns response immediately
3. Messaging worker receives event → Updates status to Processing
4. Worker simulates payment (5 seconds)
   - 50% success → Status=Completed → OrderCompleted event published
   - 50% failure → Stays in Processing
5. Background job checks every 60 seconds → Expires orders stuck in Processing for 10+ minutes

**Events:**
- `OrderCreated` - Published when order is created
- `OrderCompleted` - Published when payment succeeds
- `OrderExpired` - Published when order processing times out

**Notifications:**
- OrderCompleted → Fake email logged + database record
- OrderExpired → Database record only

### Getting Started with Part 2

#### 1. Start Infrastructure (PostgreSQL + RabbitMQ)

```bash
docker-compose up -d
```

This starts:
- PostgreSQL on port 5432
- RabbitMQ on port 5672 (AMQP)
- RabbitMQ Management UI on port 15672

**Access RabbitMQ Management UI:**
- URL: http://localhost:15672
- Username: guest
- Password: guest

#### 2. Apply Database Migrations

```bash
cd OrderProcessingSystem.WebApi
dotnet ef database update
```

This creates the new `Notifications` table.

#### 3. Run the System in Visual Studio (F5)

Press **F5** in Visual Studio to start both projects:
- OrderProcessingSystem.WebApi (Swagger UI opens automatically)
- OrderProcessingSystem.Messaging (runs in background)

**OR** run manually from command line:

Terminal 1 - Start WebApi:
```bash
cd OrderProcessingSystem.WebApi
dotnet run
```

Terminal 2 - Start Messaging Worker:
```bash
cd OrderProcessingSystem.Messaging
dotnet run
```

#### 4. Test the Event-Driven Flow

1. Login to get JWT token (via Swagger or curl)
2. Create an order:
   ```bash
   curl -X POST https://localhost:7298/api/orders \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"items":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":1}]}'
   ```
3. Order is created with status `Pending` and API returns immediately
4. Check Messaging worker logs - you'll see:
   - OrderCreated event received
   - Status updated to Processing
   - Payment simulation (5 second delay)
   - Either: OrderCompleted published OR left in Processing
5. If completed: Check Notifications table for email record
6. If still Processing: Wait 10 minutes and background job will expire it

**Query Notifications:**
```bash
docker exec -it orderprocessing_db psql -U orderuser -d OrderProcessingDB -c "SELECT * FROM \"Notifications\";"
```

### Background Jobs

**OrderExpirationBackgroundService:**
- Runs every 60 seconds
- Finds orders with status=Processing and created_at > 10 minutes ago
- Updates status to Expired
- Publishes OrderExpired event
- No external cron needed - runs inside Messaging worker

### Testing

**Run API Tests (Part 1):**
```bash
cd OrderProcessingSystem.Tests
dotnet test
```

**Run Messaging Tests (Part 2):**
```bash
cd OrderProcessingSystem.Messaging.Tests
dotnet test
```

Messaging tests use Testcontainers for both PostgreSQL and RabbitMQ, providing full integration testing with real infrastructure.

### Troubleshooting Part 2

**RabbitMQ Connection Errors:**
```bash
# Check RabbitMQ is running
docker ps | grep rabbitmq

# Restart RabbitMQ
docker-compose restart rabbitmq

# View RabbitMQ logs
docker logs orderprocessing_rabbitmq
```

**Events Not Processing:**
1. Check Messaging worker is running
2. Check RabbitMQ Management UI - verify queues exist
3. Check worker logs for exceptions
4. Verify both endpoints use same routing topology

**Background Job Not Running:**
- Check Messaging worker logs
- Background service starts automatically with worker
- First run happens immediately, then every 60 seconds

### Technologies Used (Part 2)

- **NServiceBus 9.2.4** - Messaging framework
- **NServiceBus.RabbitMQ 10.1.7** - RabbitMQ transport
- **RabbitMQ 3 Management** - Message broker
- **ASP.NET Core BackgroundService** - Recurring jobs
- **Testcontainers.RabbitMq 4.10.0** - Integration testing
```

## Validation Gates

### Build Validation
```bash
# Restore all dependencies
dotnet restore

# Build entire solution
dotnet build

# Expected: Build succeeded with 0 errors
```

### Test Validation
```bash
# Run API tests (Part 1)
cd OrderProcessingSystem.Tests
dotnet test --verbosity normal

# Run Messaging tests (Part 2)
cd OrderProcessingSystem.Messaging.Tests
dotnet test --verbosity normal

# Expected: All tests pass
```

### Runtime Validation
```bash
# 1. Start infrastructure
docker-compose up -d

# 2. Apply migrations
cd OrderProcessingSystem.WebApi
dotnet ef database update

# 3. Start WebApi (Terminal 1)
cd OrderProcessingSystem.WebApi
dotnet run

# 4. Start Messaging Worker (Terminal 2)
cd OrderProcessingSystem.Messaging
dotnet run

# 5. Expected output in Messaging worker:
# - "NServiceBus.Endpoint successfully started"
# - "OrderExpirationBackgroundService started"
# - No exceptions

# 6. Test order creation (Terminal 3)
# - Login via Swagger UI
# - Create order
# - Check Messaging worker logs for event processing
# - Verify order status changes: Pending → Processing → (Completed or stays Processing)
```

### RabbitMQ Validation
```bash
# Access Management UI
# Open browser: http://localhost:15672
# Login: guest/guest

# Expected queues:
# - OrderProcessingSystem.Messaging
# - error
# - audit (if configured)
```

## Error Handling Strategy

### Event Handler Errors
- **Immediate Retries**: 3 attempts (configured in Program.cs)
- **Delayed Retries**: 2 attempts with increasing delay
- **Error Queue**: Failed messages move to error queue after all retries
- **Logging**: All failures logged with exception details

### Background Service Errors
- **Non-Fatal**: Exceptions caught and logged, timer continues
- **Fatal**: Service stops, host restart required
- **Logging**: All exceptions logged with context

### Database Transaction Errors
- **Automatic Rollback**: Transaction pattern ensures rollback on failure
- **Event Publishing**: Only happens after successful commit
- **Idempotency**: Handlers should be idempotent (can process same message multiple times)

## Common Gotchas

### 1. NServiceBus Configuration
- ✅ Both endpoints MUST use same routing topology (ConventionalRoutingTopology)
- ✅ Both endpoints MUST use same QueueType (Quorum)
- ✅ WebApi uses SendOnly() - it publishes but doesn't handle
- ✅ Messaging worker handles all events

### 2. Event Publishing Timing
- ❌ NEVER publish events before transaction commit
- ✅ ALWAYS publish events after successful commit
- ✅ API returns immediately after publishing (no await on processing)

### 3. Background Service Scoping
- ❌ NEVER inject scoped services (DbContext) directly into BackgroundService constructor
- ✅ ALWAYS inject IServiceProvider and create scope in ExecuteAsync
- ✅ Dispose scope after each operation

### 4. Random Number Generation
- ✅ Create new Random() instance per handler invocation
- ✅ Random.Next(0, 2) returns 0 or 1 (50/50 split)
- ✅ Don't cache Random instance (can cause threading issues)

### 5. DateTime Handling
- ✅ Always use DateTime.UtcNow
- ✅ Database timestamps stored as UTC
- ✅ Expiration calculation: `DateTime.UtcNow - TimeSpan.FromMinutes(10)`

### 6. Testing with Testcontainers
- ✅ Containers start in InitializeAsync
- ✅ Containers stop in DisposeAsync
- ✅ Tests may be slow due to container startup time
- ✅ Use [Fact] not [Theory] for tests that need containers

## Implementation Checklist

- [ ] Create OrderProcessingSystem.Messaging project (Worker SDK)
- [ ] Create OrderProcessingSystem.Messaging.Tests project (xUnit)
- [ ] Add projects to solution file
- [ ] Define events (OrderCreated, OrderCompleted, OrderExpired)
- [ ] Create Notification entity in WebApi project
- [ ] Update AppDbContext with Notifications DbSet
- [ ] Create and apply AddNotificationTable migration
- [ ] Configure NServiceBus in Messaging Program.cs
- [ ] Implement OrderCreatedHandler
- [ ] Implement OrderCompletedHandler
- [ ] Implement OrderExpiredHandler
- [ ] Implement OrderExpirationBackgroundService
- [ ] Add NServiceBus to WebApi Program.cs (SendOnly)
- [ ] Modify OrderService to publish OrderCreated event
- [ ] Update docker-compose.yml with RabbitMQ service
- [ ] Create .slnLaunch file for multi-project startup
- [ ] Create MessagingIntegrationTestFactory
- [ ] Write integration tests for event handlers
- [ ] Write integration tests for background service
- [ ] Update README.md with Part 2 documentation
- [ ] Test build: `dotnet build`
- [ ] Test migrations: `dotnet ef database update`
- [ ] Test runtime: Start both projects and create order
- [ ] Verify events flow through RabbitMQ Management UI
- [ ] Verify notifications saved to database
- [ ] Run all tests: `dotnet test`

## Quality Assessment

### Confidence Level for One-Pass Implementation: 8.5/10

**Strengths:**
- ✅ Comprehensive context with all existing patterns documented
- ✅ Step-by-step implementation with exact code snippets
- ✅ Clear technology stack with specific versions
- ✅ Detailed error handling and common gotchas
- ✅ Validation gates for build, test, and runtime
- ✅ Real code references from existing codebase
- ✅ Documentation URLs for all major components

**Potential Challenges:**
- ⚠️ NServiceBus configuration has many nuances (topology, persistence, routing)
- ⚠️ Event timing and transaction coordination requires careful implementation
- ⚠️ Testcontainers tests may need retry logic for container startup
- ⚠️ Visual Studio .slnLaunch format may vary by version

**Mitigation:**
- Reference official NServiceBus docs extensively
- Follow transaction pattern from existing OrderService
- Add appropriate delays in tests
- Provide both manual and automated startup options

## Next Steps After Implementation

1. **Monitoring**: Add structured logging with Serilog
2. **Production Persistence**: Replace LearningPersistence with NServiceBus.Persistence.Sql
3. **Observability**: Add health checks for RabbitMQ and database
4. **Resilience**: Configure circuit breakers for external dependencies
5. **Performance**: Add message batching for high-volume scenarios
6. **Security**: Configure RabbitMQ with proper credentials (not guest/guest)
7. **Deployment**: Create Kubernetes manifests or Docker Compose for production
