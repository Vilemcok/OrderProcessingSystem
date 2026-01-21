# PRP: Complete Order Processing System Implementation

**Feature File**: INITIAL.md
**Target Framework**: .NET 10.0 (ASP.NET Core Web API)
**Architecture**: Clean Architecture with Controllers, Services, Repositories
**Database**: PostgreSQL with Entity Framework Core

---

## CONTEXT & OBJECTIVES

This PRP implements a complete order processing system from scratch, transforming the current barebones Web API project into a production-ready application with:
- JWT authentication
- Three CRUD modules (Users, Products, Orders)
- PostgreSQL database with EF Core migrations
- Transaction-based order processing with stock validation
- Docker-compose for PostgreSQL
- Integration tests using Testcontainers
- Swagger UI with JWT support

**Current State**: The project has a basic ASP.NET Core Web API setup with WeatherForecast demo controller, OpenAPI support, but no authentication, database, or business logic.

**End State**: A fully functional order processing system following clean architecture principles, ready to run with F5 in Visual Studio.

---

## DOCUMENTATION & RESOURCES

### Official Microsoft Documentation
- **ASP.NET Core Web API**: https://learn.microsoft.com/en-us/aspnet/core/web-api/
- **JWT Authentication**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt
- **EF Core**: https://learn.microsoft.com/en-us/ef/core/
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **EF Core Transactions**: https://learn.microsoft.com/en-us/ef/core/saving/transactions
- **Swagger/OpenAPI**: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle

### Database & PostgreSQL
- **Npgsql EF Core Provider**: https://www.npgsql.org/efcore/
- **PostgreSQL Docker**: https://hub.docker.com/_/postgres
- **Docker Compose**: https://docs.docker.com/compose/

### Testing
- **xUnit Getting Started**: https://xunit.net/docs/getting-started/netcore
- **Testcontainers for .NET**: https://dotnet.testcontainers.org/
- **Testcontainers PostgreSQL**: https://dotnet.testcontainers.org/modules/postgres/

### Additional Resources (Research Findings)
- **JWT with Swagger (.NET 10)**: https://duendesoftware.com/blog/20251126-securing-openapi-and-swagger-ui-with-oauth-in-dotnet-10
- **Testcontainers Best Practices**: https://www.milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing
- **EF Core Migrations Guide**: https://www.milanjovanovic.tech/blog/efcore-migrations-a-detailed-guide
- **EF Core Transactions**: https://learn.microsoft.com/en-us/ef/core/saving/transactions
- **Configure PostgreSQL in EF Core**: https://code-maze.com/configure-postgresql-ef-core/

---

## ARCHITECTURE OVERVIEW

### Folder Structure
```
OrderProcessingSystem/
├── Controllers/              # API Controllers (already exists)
├── Models/
│   ├── Entities/            # EF Core entities (database models)
│   └── DTOs/                # Data Transfer Objects
│       ├── Requests/        # Request DTOs
│       └── Responses/       # Response DTOs
├── Services/                # Business logic layer
│   ├── Interfaces/          # Service interfaces
│   └── Implementations/     # Service implementations
├── Repositories/            # Data access layer
│   ├── Interfaces/          # Repository interfaces
│   └── Implementations/     # Repository implementations
├── Data/                    # EF Core DbContext and configurations
│   ├── AppDbContext.cs
│   └── Configurations/      # Entity configurations
├── Middleware/              # Custom middleware (exception handling)
├── Migrations/              # EF Core migrations (auto-generated)
├── docker-compose.yml       # PostgreSQL container configuration
├── appsettings.json         # Configuration (already exists)
└── Program.cs               # Application entry point (already exists)

OrderProcessingSystem.Tests/  # New test project
├── IntegrationTests/
│   ├── AuthenticationTests.cs
│   ├── UsersTests.cs
│   ├── ProductsTests.cs
│   └── OrdersTests.cs
└── TestFixtures/
    └── TestDatabaseFixture.cs
```

### Layer Responsibilities
- **Controllers**: Handle HTTP requests, validate input, return responses
- **Services**: Contain business logic, orchestrate operations, enforce business rules
- **Repositories**: Direct database access, CRUD operations
- **DTOs**: Data contracts for API requests/responses (never expose entities)
- **Entities**: Database models with EF Core annotations
- **Middleware**: Cross-cutting concerns (exception handling)

---

## IMPLEMENTATION BLUEPRINT

### Phase 1: Database & Infrastructure Setup

#### 1.1 Install Required NuGet Packages
```bash
# Main project packages
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.2
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.2
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.2
dotnet add package Swashbuckle.AspNetCore --version 7.2.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.3.1
```

#### 1.2 Create docker-compose.yml
**Location**: Project root
**Purpose**: PostgreSQL container for local development
**Template**:
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

volumes:
  postgres_data:
```

#### 1.3 Update appsettings.json
Add connection string and JWT settings:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=OrderProcessingDB;Username=orderuser;Password=orderpass123"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyMinimum32CharactersLong!",
    "Issuer": "OrderProcessingSystem",
    "Audience": "OrderProcessingSystemUsers",
    "ExpirationHours": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Phase 2: Entity Models & Database Context

#### 2.1 Create Entity Models
**Pattern**: Use GUIDs, add validation attributes, configure relationships

**User Entity** (`Models/Entities/User.cs`):
```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

**Product Entity** (`Models/Entities/Product.cs`):
```csharp
public class Product
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

**Order Entity** (`Models/Entities/Order.cs`):
```csharp
public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Expired
}

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

#### 2.2 Create DbContext
**File**: `Data/AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed users (with pre-hashed passwords)
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        // Note: Password is "Password123!" hashed with ASP.NET Core Identity PasswordHasher
        // You'll need to generate actual hashes during implementation
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = user1Id,
                Name = "John Doe",
                Email = "john@example.com",
                PasswordHash = "PLACEHOLDER_HASH_1" // Replace with actual hash
            },
            new User
            {
                Id = user2Id,
                Name = "Jane Smith",
                Email = "jane@example.com",
                PasswordHash = "PLACEHOLDER_HASH_2" // Replace with actual hash
            }
        );

        // Seed products
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var product3Id = Guid.NewGuid();

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = product1Id,
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = 999.99m,
                Stock = 50,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = product2Id,
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = 29.99m,
                Stock = 200,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = product3Id,
                Name = "Keyboard",
                Description = "Mechanical keyboard",
                Price = 79.99m,
                Stock = 100,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
```

### Phase 3: DTOs (Data Transfer Objects)

**Critical Rule**: NEVER expose Entity classes directly in API responses or requests.

#### 3.1 Authentication DTOs
**File**: `Models/DTOs/Requests/LoginRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.DTOs.Requests;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

**File**: `Models/DTOs/Responses/LoginResponse.cs`
```csharp
namespace OrderProcessingSystem.Models.DTOs.Responses;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UserResponse User { get; set; } = null!;
}
```

#### 3.2 User DTOs
**Request**: `CreateUserRequest.cs`, `UpdateUserRequest.cs`
**Response**: `UserResponse.cs` (NO PASSWORD FIELD!)

#### 3.3 Product DTOs
**Request**: `CreateProductRequest.cs`, `UpdateProductRequest.cs`
**Response**: `ProductResponse.cs`

#### 3.4 Order DTOs
**Request**: `CreateOrderRequest.cs` with nested `OrderItemRequest[]`
**Response**: `OrderResponse.cs` with nested `OrderItemResponse[]`

**Example** (`Models/DTOs/Requests/CreateOrderRequest.cs`):
```csharp
using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.DTOs.Requests;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
```

### Phase 4: Repository Layer

#### 4.1 Generic Repository Pattern
**File**: `Repositories/Interfaces/IRepository.cs`
```csharp
namespace OrderProcessingSystem.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}
```

#### 4.2 Specific Repository Interfaces
- `IUserRepository` (add `GetByEmailAsync` method)
- `IProductRepository` (add `UpdateStockAsync` method)
- `IOrderRepository`

#### 4.3 Repository Implementations
**Pattern**: Inject `AppDbContext`, use async/await, handle not found scenarios

**Example** (`Repositories/Implementations/ProductRepository.cs`):
```csharp
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Repositories.Interfaces;

namespace OrderProcessingSystem.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product> AddAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UpdateStockAsync(Guid productId, int quantityChange)
    {
        var product = await GetByIdAsync(productId);
        if (product == null) return false;

        product.Stock += quantityChange;
        if (product.Stock < 0) return false;

        await UpdateAsync(product);
        return true;
    }
}
```

### Phase 5: Service Layer

#### 5.1 JWT Token Service
**File**: `Services/Interfaces/ITokenService.cs`
```csharp
using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
```

**Implementation** (`Services/Implementations/TokenService.cs`):
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var secret = _configuration["JwtSettings:Secret"]!;
        var issuer = _configuration["JwtSettings:Issuer"]!;
        var audience = _configuration["JwtSettings:Audience"]!;
        var expirationHours = int.Parse(_configuration["JwtSettings:ExpirationHours"]!);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### 5.2 Authentication Service
**Interface**: `IAuthService` with `LoginAsync` method
**Implementation**: Use `PasswordHasher<User>` to verify passwords

```csharp
using Microsoft.AspNetCore.Identity;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using OrderProcessingSystem.Repositories.Interfaces;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) return null;

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (verificationResult == PasswordVerificationResult.Failed)
            return null;

        var token = _tokenService.GenerateToken(user);
        var expiration = DateTime.UtcNow.AddHours(1);

        return new LoginResponse
        {
            Token = token,
            Expiration = expiration,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }
}
```

#### 5.3 User Service
**Pattern**: Map between DTOs and Entities, hash passwords on creation, never return passwords

#### 5.4 Product Service
**Pattern**: Simple CRUD operations with validation

#### 5.5 Order Service (CRITICAL - Transaction Handling)
**File**: `Services/Implementations/OrderService.cs`

**Key Requirements**:
1. Validate stock availability BEFORE creating order
2. Calculate total server-side (never trust client)
3. Use database transaction to ensure atomicity
4. Decrease stock after order creation
5. Return 400 if insufficient stock

```csharp
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
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

    public OrderService(
        AppDbContext context,
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _context = context;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
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

            // 6. Return response
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
}
```

### Phase 6: Controllers

#### 6.1 Authentication Controller
**File**: `Controllers/AuthController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);

        if (result == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(result);
    }
}
```

#### 6.2 Protected Controllers Pattern
**All other controllers** (Users, Products, Orders) must:
1. Use `[Authorize]` attribute at class level
2. Extract userId from JWT claims for operations
3. Return 400 for validation errors, 404 for not found, 401 for unauthorized

**Example** (`Controllers/OrdersController.cs`):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = Guid.Parse(User.FindFirst("userId")!.Value);

        try
        {
            var order = await _orderService.CreateOrderAsync(userId, request);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    // Implement: GetAll, Update, Delete similarly
}
```

### Phase 7: Global Exception Handling Middleware

**File**: `Middleware/GlobalExceptionHandlerMiddleware.cs`
```csharp
using System.Net;
using System.Text.Json;

namespace OrderProcessingSystem.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            message = "An internal server error occurred",
            details = exception.Message
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
```

### Phase 8: Program.cs Configuration

**File**: `Program.cs` (replace existing content)
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Middleware;
using OrderProcessingSystem.Repositories.Implementations;
using OrderProcessingSystem.Repositories.Interfaces;
using OrderProcessingSystem.Services.Implementations;
using OrderProcessingSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
var jwtAudience = builder.Configuration["JwtSettings:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

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

builder.Services.AddControllers();

// Configure Swagger with JWT support (Development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Order Processing System API",
            Version = "v1"
        });

        // Add JWT authentication to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in the text input below.\n\nExample: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program accessible to test project
public partial class Program { }
```

### Phase 9: Database Migrations

#### 9.1 Generate Password Hashes for Seed Data
Before creating migration, generate actual password hashes:

```csharp
// Temporary console app or unit test to generate hashes
var hasher = new PasswordHasher<User>();
var user = new User();
var hash1 = hasher.HashPassword(user, "Password123!");
var hash2 = hasher.HashPassword(user, "Password123!");
Console.WriteLine($"Hash 1: {hash1}");
Console.WriteLine($"Hash 2: {hash2}");
```

Update the seed data in `AppDbContext.cs` with actual hashes.

#### 9.2 Create Initial Migration
```bash
dotnet ef migrations add InitialCreate
```

#### 9.3 Apply Migration
```bash
# Make sure PostgreSQL is running via docker-compose
docker-compose up -d

# Apply migrations
dotnet ef database update
```

### Phase 10: Integration Testing

#### 10.1 Create Test Project
```bash
dotnet new xunit -n OrderProcessingSystem.Tests
cd OrderProcessingSystem.Tests
dotnet add reference ../OrderProcessingSystem/OrderProcessingSystem.csproj

# Add packages
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.0.2
dotnet add package Testcontainers.PostgreSql --version 4.3.0
dotnet add package FluentAssertions --version 7.0.0
```

#### 10.2 Test Database Fixture
**File**: `TestFixtures/TestDatabaseFixture.cs`
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessingSystem.Data;
using Testcontainers.PostgreSql;

namespace OrderProcessingSystem.Tests.TestFixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public TestDatabaseFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add test database
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString());
                    });

                    // Apply migrations
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                });
            });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

#### 10.3 Sample Integration Tests

**Authentication Test** (`IntegrationTests/AuthenticationTests.cs`):
```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using OrderProcessingSystem.Tests.TestFixtures;
using Xunit;

namespace OrderProcessingSystem.Tests.IntegrationTests;

public class AuthenticationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    public AuthenticationTests(TestDatabaseFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeEmpty();
        loginResponse.User.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "john@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Protected Endpoint Test** (`IntegrationTests/ProductsTests.cs`):
```csharp
[Fact]
public async Task GetProducts_WithoutToken_ReturnsUnauthorized()
{
    var response = await _client.GetAsync("/api/products");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetProducts_WithValidToken_ReturnsProducts()
{
    // Login first
    var loginResponse = await LoginAsTestUser();

    // Set authorization header
    _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);

    // Get products
    var response = await _client.GetAsync("/api/products");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**Order Creation with Stock Validation Test** (`IntegrationTests/OrdersTests.cs`):
```csharp
[Fact]
public async Task CreateOrder_WithInsufficientStock_ReturnsBadRequest()
{
    // Arrange
    var token = await GetAuthToken();
    _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    var orderRequest = new CreateOrderRequest
    {
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = /* product ID */, Quantity = 999999 }
        }
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/orders", orderRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().Contain("Insufficient stock");
}
```

### Phase 11: README Documentation

**File**: `README.md` (update existing or create new)
```markdown
# Order Processing System

ASP.NET Core Web API for order processing with JWT authentication, PostgreSQL database, and clean architecture.

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for PostgreSQL)

## Getting Started

### 1. Start PostgreSQL Database

```bash
docker-compose up -d
```

### 2. Apply Database Migrations

```bash
dotnet ef database update
```

This will create all tables and seed initial data:
- 2 test users (john@example.com, jane@example.com) - password: "Password123!"
- 3 sample products

### 3. Run the Application

```bash
dotnet run
```

Or press **F5** in Visual Studio.

The API will be available at:
- HTTPS: https://localhost:7298
- HTTP: http://localhost:5139
- Swagger UI: https://localhost:7298/swagger (Development only)

### 4. Test the API

#### Login to get JWT token:
```bash
curl -X POST https://localhost:7298/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"Password123!"}'
```

#### Use token to access protected endpoints:
```bash
curl https://localhost:7298/api/products \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 5. Run Integration Tests

```bash
cd OrderProcessingSystem.Tests
dotnet test
```

Tests use Testcontainers to spin up a PostgreSQL container automatically.

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email/password

### Users (Protected)
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Products (Protected)
- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Orders (Protected)
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `POST /api/orders` - Create order
- `PUT /api/orders/{id}` - Update order
- `DELETE /api/orders/{id}` - Delete order

## Architecture

- **Controllers**: Handle HTTP requests/responses
- **Services**: Business logic and orchestration
- **Repositories**: Data access layer
- **DTOs**: Data transfer objects for API contracts
- **Entities**: Database models

## Database Schema

- **Users**: id, name, email, password_hash
- **Products**: id, name, description, price, stock, created_at
- **Orders**: id, user_id, total, status, created_at, updated_at
- **OrderItems**: id, order_id, product_id, quantity, price
```

---

## CRITICAL GOTCHAS & PATTERNS

### 1. Password Hashing
- **ALWAYS** use `PasswordHasher<User>` from `Microsoft.AspNetCore.Identity`
- Hash passwords on user creation
- Use `VerifyHashedPassword` for login
- NEVER store plain text passwords

### 2. Seed Data Hashes
- Generate actual password hashes BEFORE creating migrations
- Don't use placeholder hashes in production
- Use a temporary script to generate hashes

### 3. Transaction Isolation
- Use `BeginTransactionAsync()` for order creation
- Always rollback on exceptions
- Validate ALL business rules before committing

### 4. Stock Validation
- Check stock BEFORE creating order
- Use pessimistic locking if needed for high concurrency
- Return clear error messages for insufficient stock

### 5. DTO vs Entity
- NEVER expose Entity classes in API responses
- NEVER accept Entity classes in API requests
- Always map between DTOs and Entities in Service layer

### 6. JWT Claims
- Access userId from claims: `User.FindFirst("userId")!.Value`
- Always validate token in middleware
- Use proper claim names (not "sub" but custom "userId")

### 7. Swagger Configuration
- Only enable Swagger in Development environment
- Configure Bearer authentication in Swagger
- Test authentication flow in Swagger UI

### 8. Testcontainers
- Don't hardcode connection strings
- Let Testcontainers assign dynamic ports
- Use `IAsyncLifetime` for container lifecycle

### 9. EF Core Migrations
- Always review generated migrations before applying
- Use `HasDefaultValueSql("CURRENT_TIMESTAMP")` for timestamps
- Configure unique constraints at database level

### 10. Error Responses
- Return 400 for validation errors with ModelState
- Return 404 for not found entities
- Return 401 for unauthorized (no token or invalid token)
- Return 500 for unhandled exceptions (via middleware)

---

## IMPLEMENTATION TASK CHECKLIST

Execute these tasks in order to complete the PRP implementation:

### Infrastructure & Configuration
- [ ] Install all required NuGet packages (Npgsql.EntityFrameworkCore.PostgreSQL, JwtBearer, Swashbuckle, etc.)
- [ ] Create docker-compose.yml for PostgreSQL
- [ ] Update appsettings.json with connection string and JWT settings
- [ ] Create folder structure (Models/Entities, Models/DTOs, Services, Repositories, Data, Middleware)

### Data Layer
- [ ] Create all Entity models (User, Product, Order, OrderItem)
- [ ] Create AppDbContext with entity configurations
- [ ] Generate password hashes for seed data
- [ ] Update AppDbContext with actual password hashes in seed data
- [ ] Create initial EF Core migration
- [ ] Start PostgreSQL container (docker-compose up -d)
- [ ] Apply migrations to database

### DTOs
- [ ] Create authentication DTOs (LoginRequest, LoginResponse)
- [ ] Create User DTOs (CreateUserRequest, UpdateUserRequest, UserResponse - NO PASSWORD!)
- [ ] Create Product DTOs (CreateProductRequest, UpdateProductRequest, ProductResponse)
- [ ] Create Order DTOs (CreateOrderRequest with nested items, OrderResponse)

### Repository Layer
- [ ] Create IRepository<T> generic interface
- [ ] Create specific repository interfaces (IUserRepository, IProductRepository, IOrderRepository)
- [ ] Implement UserRepository with GetByEmailAsync
- [ ] Implement ProductRepository with UpdateStockAsync
- [ ] Implement OrderRepository

### Service Layer
- [ ] Create and implement ITokenService with JWT generation
- [ ] Create and implement IAuthService with login logic
- [ ] Create and implement IUserService with password hashing
- [ ] Create and implement IProductService
- [ ] Create and implement IOrderService with transaction handling and stock validation

### API Layer
- [ ] Create AuthController with login endpoint (no [Authorize])
- [ ] Create UsersController with CRUD endpoints ([Authorize] required)
- [ ] Create ProductsController with CRUD endpoints ([Authorize] required)
- [ ] Create OrdersController with CRUD endpoints ([Authorize] required, extract userId from claims)
- [ ] Create GlobalExceptionHandlerMiddleware

### Application Configuration
- [ ] Update Program.cs with DbContext registration
- [ ] Configure JWT authentication in Program.cs
- [ ] Register all repositories and services in DI container
- [ ] Configure Swagger with JWT Bearer support (Development only)
- [ ] Add middleware pipeline (exception handler, authentication, authorization)
- [ ] Remove WeatherForecast controller and model (demo code cleanup)

### Testing
- [ ] Create OrderProcessingSystem.Tests project
- [ ] Add test packages (Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql, FluentAssertions)
- [ ] Create TestDatabaseFixture with Testcontainers setup
- [ ] Write authentication integration test (valid/invalid credentials)
- [ ] Write protected endpoint test (with/without token)
- [ ] Write order creation test with stock validation
- [ ] Write at least 2 more integration tests (5 total minimum)

### Documentation
- [ ] Update README.md with setup instructions
- [ ] Document how to start PostgreSQL
- [ ] Document how to run migrations
- [ ] Document how to run the application
- [ ] Document how to run tests
- [ ] Document API endpoints and authentication flow

### Validation
- [ ] Run `dotnet build` - should compile without errors
- [ ] Run `dotnet ef database update` - migrations should apply successfully
- [ ] Run `dotnet run` - application should start
- [ ] Test Swagger UI - should load with JWT authentication button
- [ ] Login via Swagger - should return token
- [ ] Access protected endpoint - should work with token, fail without
- [ ] Create order with insufficient stock - should return 400
- [ ] Run `dotnet test` - all integration tests should pass

---

## VALIDATION GATES

Run these commands to verify successful implementation:

### Build Validation
```bash
dotnet build
# Expected: Build succeeded. 0 Error(s)
```

### Migration Validation
```bash
docker-compose up -d
dotnet ef database update
# Expected: Applying migration '20XX_InitialCreate'. Done.
```

### Application Startup
```bash
dotnet run
# Expected: Application started at https://localhost:7298
# Swagger should be accessible at https://localhost:7298/swagger
```

### Integration Tests
```bash
cd OrderProcessingSystem.Tests
dotnet test --verbosity normal
# Expected: Passed! - Failed: 0, Passed: 5+, Skipped: 0
```

### Manual API Test
```bash
# 1. Login
curl -X POST https://localhost:7298/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"Password123!"}' \
  -k

# Expected: 200 OK with token

# 2. Access protected endpoint without token
curl https://localhost:7298/api/products -k

# Expected: 401 Unauthorized

# 3. Access protected endpoint with token
curl https://localhost:7298/api/products \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -k

# Expected: 200 OK with product list

# 4. Create order with insufficient stock
curl -X POST https://localhost:7298/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"items":[{"productId":"PRODUCT_GUID","quantity":999999}]}' \
  -k

# Expected: 400 Bad Request with "Insufficient stock" message
```

---

## ERROR HANDLING STRATEGY

### Expected Error Scenarios

1. **Validation Errors (400)**
   - Invalid DTO fields
   - Missing required fields
   - Out of range values
   - Business rule violations (insufficient stock)

2. **Authentication Errors (401)**
   - No token provided
   - Invalid/expired token
   - Invalid credentials on login

3. **Not Found Errors (404)**
   - Entity doesn't exist for given ID
   - Product not found when creating order

4. **Server Errors (500)**
   - Unhandled exceptions
   - Database connection failures
   - Caught by GlobalExceptionHandlerMiddleware

### Implementation Pattern
```csharp
// In Services
if (validationFails)
    throw new InvalidOperationException("Clear error message");

// In Controllers
try {
    var result = await _service.DoSomething();
    return Ok(result);
}
catch (InvalidOperationException ex) {
    return BadRequest(new { message = ex.Message });
}

// Unhandled exceptions caught by middleware
```

---

## PRP CONFIDENCE SCORE: 9/10

### Why 9/10?
**Strengths:**
- Comprehensive documentation with official Microsoft links
- Step-by-step implementation tasks in correct order
- Real code examples for complex patterns (transactions, JWT, Testcontainers)
- Clear validation gates
- Addresses all INITIAL.md requirements
- Includes critical gotchas and error handling
- Research-backed patterns from recent resources

**Minor Risks (-1 point):**
- First-time implementer might need to reference additional documentation for DTO mapping patterns
- Testcontainers setup might require Docker Desktop troubleshooting
- Password hashing for seed data requires a preliminary script (documented but adds a step)

**Mitigation:**
- All critical patterns have code examples
- Links to official documentation provided
- Validation gates catch issues early
- Integration tests verify end-to-end functionality

---

## FINAL NOTES

This PRP provides everything needed for one-pass implementation:
1. Complete architecture blueprint
2. Code examples for complex patterns
3. Research findings with documentation links
4. Step-by-step task checklist
5. Validation gates for verification
6. Error handling strategy

The AI agent implementing this PRP should:
- Follow the task checklist in order
- Use the code examples as templates
- Reference the documentation links when unclear
- Run validation gates after each phase
- Verify all 5+ integration tests pass
- Ensure F5 in Visual Studio works without additional configuration

**Expected implementation time for AI agent**: Single pass execution with all tasks completed and validated.
