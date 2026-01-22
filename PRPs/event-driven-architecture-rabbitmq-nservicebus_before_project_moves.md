# PRP: Event-Driven Architecture with RabbitMQ and NServiceBus (Part 2)

## Feature Overview

Extend the existing OrderProcessingSystem with event-driven architecture using RabbitMQ and NServiceBus. This implementation adds asynchronous order processing, background job expiration handling, and notification tracking while maintaining full backward compatibility with Part 1.

## Critical Constraints

**DO NOT:**
- Refactor or split the existing OrderProcessingSystem project
- Move existing controllers, EF Core code, or authentication logic
- Break or modify Part 1 functionality
- Invent features not listed in requirements
- Containerize the API in Docker Compose

**DO:**
- Add new projects instead of restructuring
- Keep backward compatibility with Part 1
- Follow all causal flows exactly as described
- Reuse existing PostgreSQL database
- Use Docker Compose ONLY for infrastructure (Postgres + RabbitMQ)

## Technology Stack (Fixed Decisions)

- **Messaging Broker**: RabbitMQ (used ONLY as transport)
- **Messaging Framework**: NServiceBus (event bus abstraction)
- **Background Jobs**: ASP.NET Core BackgroundService with PeriodicTimer
- **Database**: Existing PostgreSQL (with EF Core Migrations)
- **Testing**: xUnit + Testcontainers (PostgreSQL + RabbitMQ)
- **Worker SDK**: Microsoft.NET.Sdk.Worker

## Documentation References

### Official Documentation URLs

**NServiceBus Core:**
- Main Documentation: https://docs.particular.net/nservicebus/
- Publish-Subscribe Pattern: https://docs.particular.net/nservicebus/messaging/publish-subscribe/
- Publishing Events: https://docs.particular.net/tutorials/nservicebus-step-by-step/4-publishing-events/
- Message Handlers: https://docs.particular.net/nservicebus/handlers/

**NServiceBus RabbitMQ Transport:**
- RabbitMQ Transport: https://docs.particular.net/transports/rabbitmq/
- Simple RabbitMQ Usage: https://docs.particular.net/samples/rabbitmq/simple/
- NuGet Package: https://www.nuget.org/packages/NServiceBus.RabbitMQ (latest: 10.1.7)

**NServiceBus Testing:**
- Testing Overview: https://docs.particular.net/nservicebus/testing/
- Unit Testing: https://docs.particular.net/samples/unit-testing/

**ASP.NET Core & Worker Services:**
- Worker Services: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- Background Tasks: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-9.0
- Hosted Services: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service

**Entity Framework Core:**
- EF Core Documentation: https://learn.microsoft.com/ef/core/
- Migrations: https://learn.microsoft.com/ef/core/managing-schemas/migrations/

**Testcontainers:**
- Testcontainers for .NET: https://dotnet.testcontainers.org/
- RabbitMQ Module: https://dotnet.testcontainers.org/modules/rabbitmq/
- PostgreSQL Module: https://dotnet.testcontainers.org/modules/postgres/

**RabbitMQ:**
- Official Documentation: https://www.rabbitmq.com/docs
- .NET Client: https://www.rabbitmq.com/client-libraries.html#dotnet-csharp

**Visual Studio:**
- Multi-Project Launch: https://devblogs.microsoft.com/visualstudio/multi-project-launch-configuration/
- Multiple Startup Projects: https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects?view=visualstudio

## Existing Codebase Patterns to Follow

### 1. Project Structure Pattern

**Current Structure:**
```
OrderProcessingSystem/
├── Controllers/          # API Controllers (e.g., OrdersController.cs)
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
- `Controllers/OrdersController.cs:44-64` - Shows controller pattern with [HttpPost], async/await, JWT claims extraction
- `Services/Implementations/OrderService.cs:47-137` - Shows transaction pattern with BeginTransactionAsync, Commit/Rollback
- `Data/AppDbContext.cs:1-136` - Shows EF Core configuration, entity relationships, and seed data pattern

### 2. Entity and DbContext Pattern

**Reference: `Models/Entities/Order.cs:1-51`**
```csharp
public enum OrderStatus
{
    Pending,      // Initial state when order is created
    Processing,   // After OrderCreated event handling starts
    Completed,    // After successful payment simulation
    Expired       // After expiration background job runs
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

**Reference: `Data/AppDbContext.cs:6-16`**
```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    // ADD NEW: public DbSet<Notification> Notifications { get; set; }
}
```

**Pattern for Notification Entity:**
Follow the same pattern as Order entity with:
- Entity class in `Models/Entities/Notification.cs`
- DbSet added to AppDbContext
- OnModelCreating configuration for relationships
- EF Core migration to create table

### 3. Transaction Pattern

**Reference: `Services/Implementations/OrderService.cs:47-137`**
```csharp
public async Task<OrderResponse?> CreateOrderAsync(Guid userId, CreateOrderRequest request)
{
    // Start transaction
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Validate
        // 2. Create order with Status = Pending
        // 3. Save to database

        order.Status = OrderStatus.Pending;  // CRITICAL: Set to Pending
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Commit transaction
        await transaction.CommitAsync();

        // 4. Publish OrderCreated event (ADD THIS AFTER COMMIT)
        // await _messageSession.Publish(new OrderCreated { OrderId = order.Id });

        return MapToResponse(order);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Key Pattern:**
1. Begin transaction
2. Perform database operations
3. Commit transaction
4. **Then** publish event (after successful commit)
5. Return response immediately (NO waiting for event processing)

### 4. Dependency Injection Pattern

**Reference: `Program.cs:46-56`**
```csharp
// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

**Pattern for Part 2:**
- Add NServiceBus message session to DI
- Register it as singleton or scoped depending on NServiceBus requirements
- Inject IMessageSession into OrderService for publishing events

### 5. Integration Testing Pattern

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
        dbContext.Orders.RemoveRange(dbContext.Orders);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        // Seed test data
    }
}
```

**Reference: `OrderProcessingSystem.Tests/OrderProcessingSystem.Tests.csproj:17`**
- Uses Testcontainers.PostgreSql 4.10.0

**Pattern for Messaging Tests:**
- Use same IClassFixture pattern
- Add Testcontainers.RabbitMq 4.10.0
- Create IntegrationTestWebAppFactory for messaging endpoint
- Test event handlers with real RabbitMQ and PostgreSQL containers

### 6. Migration Pattern

**Reference: Existing migrations in `Migrations/` folder**
```bash
dotnet ef migrations add AddNotificationTable
dotnet ef database update
```

**Pattern:**
- Migrations are in the main OrderProcessingSystem project
- Use descriptive names: `AddNotificationTable`
- Always test migration up and down

## Implementation Blueprint (Pseudocode)

### Phase 1: Project Setup

#### Step 1.1: Create OrderProcessingSystem.Messaging Project
```bash
dotnet new worker -n OrderProcessingSystem.Messaging
# Edit .csproj to ensure SDK="Microsoft.NET.Sdk.Worker"
# Set TargetFramework to net10.0
```

**Add NuGet Packages:**
```xml
<PackageReference Include="NServiceBus" Version="9.2.4" />
<PackageReference Include="NServiceBus.RabbitMQ" Version="10.1.7" />
<PackageReference Include="NServiceBus.Extensions.Hosting" Version="3.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.2" />
```

**Add Project Reference:**
```xml
<ProjectReference Include="..\OrderProcessingSystem\OrderProcessingSystem.csproj" />
```

#### Step 1.2: Create OrderProcessingSystem.Messaging.Tests Project
```bash
dotnet new xunit -n OrderProcessingSystem.Messaging.Tests
```

**Add NuGet Packages:**
```xml
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.10.0" />
<PackageReference Include="Testcontainers.RabbitMq" Version="4.10.0" />
<PackageReference Include="NServiceBus.Testing" Version="9.2.0" />
```

#### Step 1.3: Create Solution File
```bash
# Create .sln if it doesn't exist
dotnet new sln -n OrderProcessingSystem
dotnet sln add OrderProcessingSystem/OrderProcessingSystem.csproj
dotnet sln add OrderProcessingSystem.Tests/OrderProcessingSystem.Tests.csproj
dotnet sln add OrderProcessingSystem.Messaging/OrderProcessingSystem.Messaging.csproj
dotnet sln add OrderProcessingSystem.Messaging.Tests/OrderProcessingSystem.Messaging.Tests.csproj
```

### Phase 2: Define Integration Events

#### Step 2.1: Create Events Folder Structure
```
OrderProcessingSystem.Messaging/
├── Events/
│   ├── OrderCreated.cs
│   ├── OrderCompleted.cs
│   └── OrderExpired.cs
```

#### Step 2.2: Define Events (Minimal Data Pattern)
```csharp
// Events/OrderCreated.cs
using NServiceBus;

namespace OrderProcessingSystem.Messaging.Events;

public class OrderCreated : IEvent
{
    public Guid OrderId { get; set; }
}

// Events/OrderCompleted.cs
public class OrderCompleted : IEvent
{
    public Guid OrderId { get; set; }
}

// Events/OrderExpired.cs
public class OrderExpired : IEvent
{
    public Guid OrderId { get; set; }
}
```

**Key Pattern:**
- Events implement `NServiceBus.IEvent`
- Events contain only necessary data (OrderId)
- Events are named in past tense
- Events represent something that has already happened

### Phase 3: Configure NServiceBus Worker

#### Step 3.1: Configure Program.cs in Messaging Project

**Reference: NServiceBus Worker Service Pattern**
```csharp
// OrderProcessingSystem.Messaging/Program.cs
using OrderProcessingSystem.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Configure NServiceBus
builder.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("OrderProcessingSystem.Messaging");

    // Use RabbitMQ transport
    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
    transport.ConnectionString("host=localhost;username=guest;password=guest");
    transport.UseConventionalRoutingTopology(QueueType.Quorum);

    // Use Learning persistence for development (or SQL for production)
    // For production with PostgreSQL, use NServiceBus.Persistence.Sql
    endpointConfiguration.UsePersistence<LearningPersistence>();

    // Enable installers for development
    endpointConfiguration.EnableInstallers();

    // Configure serialization
    endpointConfiguration.UseSerialization<SystemJsonSerializer>();

    return endpointConfiguration;
});

// Register AppDbContext for database access
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register background services
builder.Services.AddHostedService<OrderExpirationBackgroundService>();

var host = builder.Build();
await host.RunAsync();
```

**Critical Configuration Notes:**
- Endpoint name: "OrderProcessingSystem.Messaging"
- RabbitMQ connection string: from appsettings.json or default localhost
- ConventionalRoutingTopology with QueueType.Quorum (recommended for new deployments)
- LearningPersistence for simplicity (consider SQL persistence for production)
- EnableInstallers() for automatic queue creation

### Phase 4: Implement Event Handlers

#### Step 4.1: OrderCreatedHandler

**Create: `Handlers/OrderCreatedHandler.cs`**
```csharp
using NServiceBus;
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Messaging.Events;

namespace OrderProcessingSystem.Messaging.Handlers;

public class OrderCreatedHandler : IHandleMessages<OrderCreated>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(AppDbContext dbContext, ILogger<OrderCreatedHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderCreated message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Handling OrderCreated event for OrderId: {OrderId}", message.OrderId);

        // 1. Update order status: pending → processing
        var order = await _dbContext.Orders.FindAsync(message.OrderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", message.OrderId);
            return;
        }

        order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} status updated to Processing", message.OrderId);

        // 2. Simulate payment processing
        await Task.Delay(5000);

        // 3. Randomly decide outcome (50/50)
        var random = new Random();
        var isSuccess = random.Next(0, 2) == 1; // 0 or 1

        if (isSuccess)
        {
            // 50%: processing → completed
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Publish OrderCompleted event
            await context.Publish(new OrderCompleted { OrderId = order.Id });
            _logger.LogInformation("Order {OrderId} completed successfully", message.OrderId);
        }
        else
        {
            // 50%: leave as processing
            _logger.LogInformation("Order {OrderId} left in Processing state", message.OrderId);
        }
    }
}
```

**Key Patterns:**
- Implement `IHandleMessages<OrderCreated>`
- Inject AppDbContext via constructor (scoped service)
- Use `IMessageHandlerContext.Publish()` to publish events
- Async/await for all operations
- Proper logging for observability

#### Step 4.2: OrderCompletedHandler

**Create: `Handlers/OrderCompletedHandler.cs`**
```csharp
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Messaging.Events;

namespace OrderProcessingSystem.Messaging.Handlers;

public class OrderCompletedHandler : IHandleMessages<OrderCompleted>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderCompletedHandler> _logger;

    public OrderCompletedHandler(AppDbContext dbContext, ILogger<OrderCompletedHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderCompleted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Handling OrderCompleted event for OrderId: {OrderId}", message.OrderId);

        // 1. Log fake email notification to console
        _logger.LogInformation("FAKE EMAIL: Order {OrderId} has been completed!", message.OrderId);
        Console.WriteLine($"[EMAIL NOTIFICATION] Order {message.OrderId} has been completed!");

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

        _logger.LogInformation("Notification saved for OrderId: {OrderId}", message.OrderId);
    }
}
```

#### Step 4.3: OrderExpiredHandler

**Create: `Handlers/OrderExpiredHandler.cs`**
```csharp
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Messaging.Events;

namespace OrderProcessingSystem.Messaging.Handlers;

public class OrderExpiredHandler : IHandleMessages<OrderExpired>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrderExpiredHandler> _logger;

    public OrderExpiredHandler(AppDbContext dbContext, ILogger<OrderExpiredHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(OrderExpired message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Handling OrderExpired event for OrderId: {OrderId}", message.OrderId);

        // Save notification record to database (no email needed)
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Type = NotificationType.Expired,
            Message = $"Order {message.OrderId} has expired due to timeout.",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Notification saved for expired OrderId: {OrderId}", message.OrderId);
    }
}
```

### Phase 5: Implement Background Expiration Job

#### Step 5.1: Create OrderExpirationBackgroundService

**Reference: ASP.NET Core BackgroundService with PeriodicTimer**

**Create: `BackgroundServices/OrderExpirationBackgroundService.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Messaging.Events;
using NServiceBus;

namespace OrderProcessingSystem.Messaging.BackgroundServices;

public class OrderExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpirationBackgroundService> _logger;
    private readonly IMessageSession _messageSession;

    public OrderExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OrderExpirationBackgroundService> logger,
        IMessageSession messageSession)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messageSession = messageSession;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationBackgroundService started");

        // Use PeriodicTimer for recurring execution every 60 seconds
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await ProcessExpiredOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OrderExpirationBackgroundService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OrderExpirationBackgroundService");
            }
        }

        _logger.LogInformation("OrderExpirationBackgroundService stopped");
    }

    private async Task ProcessExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for expired orders...");

        // Create a scope to get scoped services (DbContext)
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Find orders with:
        // - status = 'processing'
        // - created_at older than 10 minutes
        var expirationThreshold = DateTime.UtcNow.AddMinutes(-10);

        var expiredOrders = await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Processing && o.CreatedAt < expirationThreshold)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} expired orders", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            // Update status to 'expired'
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;

            // Publish OrderExpired event
            await _messageSession.Publish(new OrderExpired { OrderId = order.Id });

            _logger.LogInformation("Order {OrderId} expired and event published", order.Id);
        }

        if (expiredOrders.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
```

**Key Patterns:**
- Inherit from `BackgroundService`
- Override `ExecuteAsync` method
- Use `PeriodicTimer` for recurring execution (modern approach)
- Create scope with `IServiceProvider.CreateScope()` for scoped services
- Inject `IMessageSession` to publish events
- Handle `OperationCanceledException` for graceful shutdown
- Proper error handling and logging

### Phase 6: Modify OrderService to Publish Events

#### Step 6.1: Update OrderProcessingSystem Project

**Add NuGet Package to OrderProcessingSystem.csproj:**
```xml
<PackageReference Include="NServiceBus" Version="9.2.4" />
```

**Modify: `Services/Implementations/OrderService.cs`**

**Add field:**
```csharp
private readonly IMessageSession _messageSession;
```

**Update constructor:**
```csharp
public OrderService(
    AppDbContext context,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IMessageSession messageSession)
{
    _context = context;
    _orderRepository = orderRepository;
    _productRepository = productRepository;
    _messageSession = messageSession;
}
```

**Modify CreateOrderAsync (after line 112 - after transaction.CommitAsync()):**
```csharp
// Commit transaction
await transaction.CommitAsync();

// 3. Publish OrderCreated event (AFTER successful commit)
// IMPORTANT: Do NOT await event processing - return immediately
await _messageSession.Publish(new OrderCreated { OrderId = order.Id });

// 4. Return response immediately
return new OrderResponse { ... };
```

**Add using statement:**
```csharp
using OrderProcessingSystem.Messaging.Events;
```

**CRITICAL FLOW:**
1. Transaction committed → database has order with status=Pending
2. Event published → asynchronous processing starts
3. Response returned → API responds immediately
4. NO waiting, NO status updates beyond Pending in the controller

### Phase 7: Add Notification Entity and Migration

#### Step 7.1: Create Notification Entity

**Create: `Models/Entities/Notification.cs` in OrderProcessingSystem project**
```csharp
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
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Order Order { get; set; } = null!;
}
```

#### Step 7.2: Update AppDbContext

**Modify: `Data/AppDbContext.cs`**

**Add DbSet:**
```csharp
public DbSet<Notification> Notifications { get; set; }
```

**Add to OnModelCreating (after OrderItem configuration):**
```csharp
// Notification configuration
modelBuilder.Entity<Notification>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Type).HasConversion<string>();
    entity.Property(e => e.Message).IsRequired();
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

    entity.HasOne(e => e.Order)
        .WithMany()
        .HasForeignKey(e => e.OrderId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

#### Step 7.3: Create Migration

```bash
cd OrderProcessingSystem
dotnet ef migrations add AddNotificationTable
```

**Verify migration creates:**
- Notifications table
- Foreign key to Orders table
- Proper column types (Type as varchar, Message as text)

### Phase 8: Update Docker Compose

**Modify: `docker-compose.yml`**
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
    image: rabbitmq:3-management
    container_name: orderprocessing_rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI port
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  postgres_data:
  rabbitmq_data:
```

**Key Points:**
- Use `rabbitmq:3-management` image for management UI
- Expose port 5672 (AMQP) and 15672 (Management UI)
- Default credentials: guest/guest
- RabbitMQ Management UI accessible at http://localhost:15672

### Phase 9: Configure Visual Studio Multi-Project Startup

#### Step 9.1: Create .slnLaunch File

**Create: `OrderProcessingSystem.slnLaunch` in solution root**
```json
{
  "version": 1,
  "defaults": {},
  "configurations": [
    {
      "name": "API + Messaging Worker",
      "projects": [
        {
          "name": "OrderProcessingSystem\\OrderProcessingSystem.csproj",
          "launchProfile": "https"
        },
        {
          "name": "OrderProcessingSystem.Messaging\\OrderProcessingSystem.Messaging.csproj",
          "launchProfile": "OrderProcessingSystem.Messaging"
        }
      ]
    }
  ]
}
```

**Alternative: Using Solution Properties (Visual Studio UI)**
1. Right-click solution → Properties
2. Startup Project → Multiple startup projects
3. Set Action = Start for both:
   - OrderProcessingSystem
   - OrderProcessingSystem.Messaging
4. Save configuration

**F5 Experience:**
- Pressing F5 starts both projects
- Swagger UI opens automatically for API
- Worker service runs in background processing events

### Phase 10: Integration Testing

#### Step 10.1: Create Test Infrastructure

**Create: `OrderProcessingSystem.Messaging.Tests/MessagingIntegrationTestBase.cs`**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OrderProcessingSystem.Messaging.Tests;

public class MessagingIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitmqContainer;
    protected IServiceProvider ServiceProvider = null!;

    public MessagingIntegrationTestBase()
    {
        // Configure PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("OrderProcessingDB_Test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        // Configure RabbitMQ container
        _rabbitmqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start containers
        await _postgresContainer.StartAsync();
        await _rabbitmqContainer.StartAsync();

        // Setup service provider
        var services = new ServiceCollection();

        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));

        // Configure NServiceBus endpoint for testing
        // ... (configure test endpoint)

        ServiceProvider = services.BuildServiceProvider();

        // Apply migrations
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitmqContainer.DisposeAsync();
    }

    protected async Task<AppDbContext> GetDbContextAsync()
    {
        var scope = ServiceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
```

#### Step 10.2: Create Test Cases

**Create: `OrderProcessingSystem.Messaging.Tests/OrderCreatedHandlerTests.cs`**
```csharp
using FluentAssertions;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Messaging.Events;
using OrderProcessingSystem.Messaging.Handlers;
using NServiceBus.Testing;

namespace OrderProcessingSystem.Messaging.Tests;

public class OrderCreatedHandlerTests : MessagingIntegrationTestBase
{
    [Fact]
    public async Task Handle_OrderCreated_UpdatesStatusToProcessing()
    {
        // Arrange
        var dbContext = await GetDbContextAsync();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Total = 100m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var handler = new OrderCreatedHandler(dbContext, LoggerFactory.Create(builder => {}).CreateLogger<OrderCreatedHandler>());
        var message = new OrderCreated { OrderId = orderId };
        var context = new TestableMessageHandlerContext();

        // Act
        await handler.Handle(message, context);

        // Assert
        var updatedOrder = await dbContext.Orders.FindAsync(orderId);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().BeOneOf(OrderStatus.Processing, OrderStatus.Completed);
    }
}
```

**Create: `OrderProcessingSystem.Messaging.Tests/NotificationPersistenceTests.cs`**
```csharp
public class NotificationPersistenceTests : MessagingIntegrationTestBase
{
    [Fact]
    public async Task OrderCompletedHandler_SavesNotification()
    {
        // Test that notification is saved to database
    }

    [Fact]
    public async Task OrderExpiredHandler_SavesNotification()
    {
        // Test that notification is saved to database
    }
}
```

**Create: `OrderProcessingSystem.Messaging.Tests/OrderExpirationBackgroundServiceTests.cs`**
```csharp
public class OrderExpirationBackgroundServiceTests : MessagingIntegrationTestBase
{
    [Fact]
    public async Task ProcessExpiredOrders_ExpiresOldProcessingOrders()
    {
        // Create order older than 10 minutes with Processing status
        // Run expiration logic
        // Assert status changed to Expired
        // Assert OrderExpired event published
    }

    [Fact]
    public async Task ProcessExpiredOrders_DoesNotExpireRecentOrders()
    {
        // Create order less than 10 minutes old with Processing status
        // Run expiration logic
        // Assert status still Processing
    }
}
```

#### Step 10.3: Test Execution

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run specific test project
dotnet test OrderProcessingSystem.Messaging.Tests/OrderProcessingSystem.Messaging.Tests.csproj
```

### Phase 11: Update README

**Modify: `README.md`**

Add new sections:

**Part 2: Event-Driven Architecture**
- Overview of event-driven flow
- RabbitMQ + NServiceBus architecture
- Background job explanation
- How to start infrastructure (docker-compose up -d)
- How to run migrations (dotnet ef database update)
- How to start solution in Visual Studio (F5)
- Event flow diagram (OrderCreated → OrderCompleted/Expired)
- Notification tracking explanation
- RabbitMQ Management UI (http://localhost:15672)

## Validation Gates (AI Must Execute These)

After completing implementation, run these commands sequentially:

### 1. Build Validation
```bash
# Build solution
dotnet build

# Expected: No errors, all projects build successfully
```

### 2. Migration Validation
```bash
# Apply migration
dotnet ef database update --project OrderProcessingSystem

# Expected: Notifications table created successfully
```

### 3. Infrastructure Validation
```bash
# Start infrastructure
docker-compose up -d

# Expected: PostgreSQL and RabbitMQ containers running
# Verify: docker ps should show both containers

# Verify RabbitMQ Management UI
# Expected: http://localhost:15672 accessible (guest/guest)
```

### 4. Test Validation
```bash
# Run all tests
dotnet test

# Expected: All tests pass (both Part 1 and Part 2 tests)
```

### 5. Runtime Validation
```bash
# Start both projects (if not using Visual Studio F5)
# Terminal 1:
cd OrderProcessingSystem
dotnet run

# Terminal 2:
cd OrderProcessingSystem.Messaging
dotnet run

# Expected:
# - API running on https://localhost:7298
# - Swagger UI accessible
# - Worker service running and connecting to RabbitMQ
# - No errors in logs
```

### 6. End-to-End Flow Validation

**Manual Test Flow:**
```bash
# 1. Login to get JWT token
curl -X POST https://localhost:7298/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"Password123!"}' \
  -k

# 2. Create an order (should return with status="Pending")
curl -X POST https://localhost:7298/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{"items":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":1}]}' \
  -k

# 3. Wait 5 seconds and check order status
# Expected: Status changed to either "Processing" or "Completed" (50/50 chance)

# 4. Check notifications table
# Expected: If completed, notification record exists

# 5. For expiration test: Create order and wait 10+ minutes
# Expected: Background job changes status to "Expired" and creates notification
```

## Error Handling Strategy

### Expected Issues and Solutions

**1. NServiceBus Connection Errors**
- Issue: Cannot connect to RabbitMQ
- Solution: Ensure docker-compose is running, verify port 5672 is accessible
- Verify: Check RabbitMQ logs with `docker logs orderprocessing_rabbitmq`

**2. Migration Errors**
- Issue: Migration fails due to existing tables
- Solution: Drop database and recreate: `dotnet ef database drop --force && dotnet ef database update`

**3. Dependency Injection Errors**
- Issue: Cannot resolve IMessageSession
- Solution: Ensure NServiceBus is configured in Program.cs with `.UseNServiceBus()`

**4. Scoped Service Errors in BackgroundService**
- Issue: Cannot consume scoped service from singleton
- Solution: Use `IServiceProvider.CreateScope()` pattern

**5. Event Not Being Processed**
- Issue: Event published but handler not invoked
- Solution: Check RabbitMQ Management UI, verify queue creation, check handler namespace

## Implementation Checklist (For AI to Track)

### Project Structure
- [ ] Create OrderProcessingSystem.Messaging project (Microsoft.NET.Sdk.Worker)
- [ ] Create OrderProcessingSystem.Messaging.Tests project
- [ ] Create solution file (.sln)
- [ ] Add all projects to solution

### Event Definitions
- [ ] Create Events folder
- [ ] Define OrderCreated event (implements IEvent)
- [ ] Define OrderCompleted event (implements IEvent)
- [ ] Define OrderExpired event (implements IEvent)

### NServiceBus Configuration
- [ ] Add NuGet packages to Messaging project
- [ ] Configure Program.cs with UseNServiceBus
- [ ] Configure RabbitMQ transport
- [ ] Configure persistence (Learning or SQL)
- [ ] Enable installers

### Event Handlers
- [ ] Implement OrderCreatedHandler
  - [ ] Update status to Processing
  - [ ] Simulate 5-second delay
  - [ ] Random 50/50 outcome
  - [ ] Publish OrderCompleted on success
- [ ] Implement OrderCompletedHandler
  - [ ] Log fake email to console
  - [ ] Save Notification to database
- [ ] Implement OrderExpiredHandler
  - [ ] Save Notification to database

### Background Service
- [ ] Implement OrderExpirationBackgroundService
  - [ ] Use PeriodicTimer (60 seconds)
  - [ ] Query orders older than 10 minutes with Processing status
  - [ ] Update status to Expired
  - [ ] Publish OrderExpired event
- [ ] Register as HostedService

### Database Changes
- [ ] Create Notification entity
- [ ] Add DbSet to AppDbContext
- [ ] Configure entity relationships
- [ ] Create migration (AddNotificationTable)
- [ ] Test migration up/down

### OrderService Modification
- [ ] Add NServiceBus package to API project
- [ ] Inject IMessageSession into OrderService
- [ ] Publish OrderCreated event after transaction commit
- [ ] Verify NO waiting for event processing
- [ ] Verify API returns with status=Pending

### Docker Compose
- [ ] Add RabbitMQ service with management plugin
- [ ] Expose ports 5672 and 15672
- [ ] Configure volumes for persistence
- [ ] Test containers start successfully

### Visual Studio Configuration
- [ ] Create .slnLaunch file for multi-project startup
- [ ] Configure both projects to start on F5
- [ ] Verify Swagger UI opens automatically

### Testing
- [ ] Create MessagingIntegrationTestBase with Testcontainers
- [ ] Add PostgreSQL container configuration
- [ ] Add RabbitMQ container configuration
- [ ] Write OrderCreatedHandler tests
- [ ] Write NotificationPersistence tests
- [ ] Write OrderExpiration background service tests
- [ ] Verify all tests pass

### Documentation
- [ ] Update README with Part 2 architecture
- [ ] Add event flow documentation
- [ ] Add docker-compose instructions
- [ ] Add migration instructions
- [ ] Add Visual Studio F5 instructions
- [ ] Add background job explanation
- [ ] Add RabbitMQ Management UI URL

### Validation
- [ ] Run `dotnet build` - all projects compile
- [ ] Run `dotnet ef database update` - migration applied
- [ ] Run `docker-compose up -d` - infrastructure running
- [ ] Run `dotnet test` - all tests pass
- [ ] Test end-to-end flow manually
- [ ] Verify RabbitMQ Management UI accessible

## Critical Success Criteria

**Must Have:**
1. API returns immediately with status=Pending (NO waiting)
2. OrderCreated event published AFTER transaction commit
3. Event handlers run asynchronously
4. Background job runs every 60 seconds
5. Notifications saved to database
6. All Part 1 tests still pass
7. RabbitMQ containers run locally (not API)
8. Multi-project startup works with F5

**Must NOT Have:**
1. NO refactoring of existing OrderProcessingSystem project
2. NO breaking changes to Part 1 functionality
3. NO synchronous event handling in controllers
4. NO API containerization in docker-compose.yml
5. NO invented features beyond requirements

## Additional Resources

**Code Examples from Research:**
- NServiceBus Simple RabbitMQ: https://docs.particular.net/samples/rabbitmq/simple/
- Worker Service Template: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- Testcontainers RabbitMQ: https://dotnet.testcontainers.org/modules/rabbitmq/

**Community Resources:**
- NServiceBus GitHub: https://github.com/Particular/NServiceBus
- NServiceBus RabbitMQ Transport: https://github.com/Particular/NServiceBus.RabbitMQ
- Testcontainers .NET: https://github.com/testcontainers/testcontainers-dotnet

## PRP Confidence Score: 9/10

**Rationale:**
- **Strengths:**
  - Comprehensive context from existing codebase patterns
  - Clear implementation blueprint with pseudocode
  - Specific file references and line numbers
  - Executable validation gates
  - Error handling strategy
  - Official documentation URLs
  - Testcontainers patterns from existing tests
  - No ambiguity in requirements

- **Potential Risk (1 point deduction):**
  - NServiceBus configuration in Worker Service context may require minor adjustments based on specific NServiceBus version nuances
  - Scoped service injection in handlers might need verification (DbContext lifetime in NServiceBus handlers)

**Expected Outcome:** One-pass implementation success with AI able to self-validate through executable validation gates.
