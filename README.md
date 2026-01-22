# Order Processing System

ASP.NET Core Web API (.NET 10.0) for order processing with JWT authentication, PostgreSQL database, and clean architecture.

## Features

### Part 1: Core API
- **JWT Authentication** - Secure token-based authentication
- **Clean Architecture** - Controllers, Services, Repositories pattern
- **PostgreSQL Database** - Entity Framework Core 10 with migrations
- **Transaction-Based Order Processing** - Stock validation with database transactions
- **Swagger UI** - Interactive API documentation (Development only)
- **Global Exception Handling** - Centralized error handling middleware

### Part 2: Event-Driven Architecture
- **Asynchronous Order Processing** - Background event handling with NServiceBus
- **RabbitMQ Messaging** - Reliable message broker for event transport
- **Background Job Expiration** - Automatic order timeout handling
- **Notification System** - Database-backed notification tracking
- **Multi-Project Architecture** - Separate API and messaging worker services

## Prerequisites

- **.NET 10.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

## Getting Started

### 1. Start PostgreSQL Database

```bash
docker-compose up -d
```

This will start a PostgreSQL container on port 5432 with the following credentials:
- **Database**: OrderProcessingDB
- **Username**: orderuser
- **Password**: orderpass123

### 2. Apply Database Migrations

```bash
dotnet ef database update
```

This creates all tables and seeds initial data:
- **2 test users**: john@example.com, jane@example.com (password: `Password123!`)
- **3 sample products**: Laptop ($999.99), Mouse ($29.99), Keyboard ($79.99)

### 3. Run the Application

```bash
dotnet run
```

Or press **F5** in Visual Studio.

The API will be available at:
- **HTTP**: http://localhost:5139
- **HTTPS**: https://localhost:7298
- **Swagger UI**: https://localhost:7298/swagger (Development only)

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

#### 3. Run the System

**Option A: Visual Studio (F5)**
Press **F5** in Visual Studio to start both projects:
- OrderProcessingSystem.WebApi (Swagger UI opens automatically)
- OrderProcessingSystem.Messaging (runs in background)

**Option B: Manual from Command Line**

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
     -d '{"items":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":1}]}' \
     -k
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

## API Endpoints

### Authentication

**POST** `/api/auth/login` - Login with email/password

**Request**:
```json
{
  "email": "john@example.com",
  "password": "Password123!"
}
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-01-21T10:00:00Z",
  "user": {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "John Doe",
    "email": "john@example.com"
  }
}
```

### Users (Protected - Requires JWT)

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Products (Protected - Requires JWT)

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create new product
- `PUT /api/products/{id}` - Update product
- `DELETE /api/products/{id}` - Delete product

### Orders (Protected - Requires JWT)

- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get order by ID
- `GET /api/orders/user/{userId}` - Get orders by user ID
- `POST /api/orders` - Create new order
- `DELETE /api/orders/{id}` - Delete order

## Using the API

### 1. Login to Get JWT Token

```bash
curl -X POST https://localhost:7298/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@example.com","password":"Password123!"}' \
  -k
```

### 2. Access Protected Endpoints

Use the token from login response in the Authorization header:

```bash
curl https://localhost:7298/api/products \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -k
```

### 3. Create an Order

```bash
curl -X POST https://localhost:7298/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "items": [
      {
        "productId": "33333333-3333-3333-3333-333333333333",
        "quantity": 2
      }
    ]
  }' \
  -k
```

## Using Swagger UI

1. Navigate to https://localhost:7298/swagger
2. Click **Authorize** button
3. Login using `/api/auth/login` to get a token
4. Copy the token value (without "Bearer" prefix)
5. Paste token in the authorization dialog
6. Click **Authorize**
7. Try the protected endpoints

## Architecture

### Project Structure

```
OrderProcessingSystem/
├── OrderProcessingSystem.WebApi/
│   ├── Controllers/          # API Controllers
│   ├── Models/
│   │   ├── Entities/        # Database entities (Order, User, Product, Notification)
│   │   └── DTOs/            # Data Transfer Objects
│   │       ├── Requests/    # Request DTOs
│   │       └── Responses/   # Response DTOs
│   ├── Events/              # NServiceBus event definitions (Part 2)
│   ├── Services/            # Business logic layer
│   │   ├── Interfaces/
│   │   └── Implementations/
│   ├── Repositories/        # Data access layer
│   │   ├── Interfaces/
│   │   └── Implementations/
│   ├── Data/                # EF Core DbContext
│   ├── Middleware/          # Custom middleware
│   └── Migrations/          # EF Core migrations
├── OrderProcessingSystem.Messaging/ (Part 2)
│   ├── Handlers/            # NServiceBus event handlers
│   ├── BackgroundJobs/      # Background services (OrderExpirationBackgroundService)
│   └── Program.cs           # Worker host configuration
├── OrderProcessingSystem.Tests/
│   └── Integration/         # API integration tests
└── OrderProcessingSystem.Messaging.Tests/ (Part 2)
    └── Integration/         # Messaging integration tests with Testcontainers
```

### Layer Responsibilities

- **Controllers**: Handle HTTP requests, validate input, return responses
- **Services**: Business logic, orchestrate operations, enforce business rules
- **Repositories**: Database access, CRUD operations
- **DTOs**: API contracts (never expose entities directly)
- **Entities**: Database models with EF Core annotations
- **Middleware**: Cross-cutting concerns (exception handling)

### Database Schema

**Users**
- id (guid, PK)
- name (varchar 100)
- email (varchar 100, unique)
- password_hash (text)

**Products**
- id (guid, PK)
- name (varchar 100)
- description (text)
- price (decimal 18,2)
- stock (int)
- created_at (timestamp)

**Orders**
- id (guid, PK)
- user_id (guid, FK)
- total (decimal 18,2)
- status (enum: Pending, Processing, Completed, Expired)
- created_at (timestamp)
- updated_at (timestamp)

**OrderItems**
- id (guid, PK)
- order_id (guid, FK)
- product_id (guid, FK)
- quantity (int)
- price (decimal 18,2)

**Notifications** (Part 2)
- id (guid, PK)
- order_id (guid, FK)
- type (enum: Completed, Expired)
- message (varchar 500)
- created_at (timestamp)

## Key Features Explained

### JWT Authentication

- Token-based authentication with 1-hour expiration
- Tokens include userId, email, and name claims
- All endpoints except `/api/auth/login` require authentication
- Configure in `appsettings.json` under `JwtSettings`

### Order Processing with Transactions

Order creation uses database transactions to ensure:
1. Stock availability is validated before order creation
2. Total is calculated server-side (never trust client)
3. Stock is decreased atomically with order creation
4. Rollback occurs if any step fails

### Password Security

- Passwords are hashed using ASP.NET Core Identity's `PasswordHasher<T>`
- Passwords are never stored in plain text
- Passwords are never returned in API responses

### Global Exception Handling

All unhandled exceptions are caught by `GlobalExceptionHandlerMiddleware` and returned as:
```json
{
  "message": "An internal server error occurred",
  "details": "Exception details..."
}
```

## Development

### Add a New Migration

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Remove Last Migration

```bash
dotnet ef migrations remove
```

### Reset Database

```bash
dotnet ef database drop
dotnet ef database update
```

### Stop PostgreSQL

```bash
docker-compose down
```

### View PostgreSQL Data

```bash
docker exec -it orderprocessing_db psql -U orderuser -d OrderProcessingDB
```

## Configuration

### appsettings.json

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
  }
}
```

**Note**: For production, store secrets in Azure Key Vault, AWS Secrets Manager, or similar.

## Troubleshooting

### Database Connection Errors

Ensure PostgreSQL is running:
```bash
docker ps | grep orderprocessing_db
```

If not running:
```bash
docker-compose up -d
```

### Port Already in Use

Change ports in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:YOUR_PORT;http://localhost:YOUR_PORT"
```

### Migration Errors

Drop and recreate database:
```bash
dotnet ef database drop --force
dotnet ef database update
```

## Technologies Used

### Part 1: Core API
- **.NET 10.0** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core 10** - ORM
- **PostgreSQL** - Database
- **Npgsql** - PostgreSQL provider for EF Core
- **JWT Bearer Authentication** - Security
- **Swashbuckle (Swagger)** - API documentation
- **Docker** - Containerization

### Part 2: Event-Driven Architecture
- **NServiceBus 9.2.7** - Messaging framework
- **NServiceBus.RabbitMQ 10.1.7** - RabbitMQ transport
- **RabbitMQ 3 Management** - Message broker
- **ASP.NET Core BackgroundService** - Recurring jobs
- **Testcontainers.RabbitMq 4.10.0** - Integration testing

## License

This project is for educational purposes.
