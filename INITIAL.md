## FEATURE:

You are extending an existing solution.
DO NOT refactor or break Part 1.

CURRENT STATE:
- The solution already contains:
  - OrderProcessingSystem (ASP.NET Core Web API, Controllers, EF Core, Auth, Swagger)
  - OrderProcessingSystem.Tests (xUnit + Testcontainers, tests for Part 1)
- Part 1 functionality MUST remain fully working and unchanged.

GOAL:
Implement Part 2: Event-Driven Architecture + Background Processing
using RabbitMQ and NServiceBus.

IMPORTANT RULES:
- Do NOT refactor or split existing OrderProcessingSystem project
- Do NOT move existing controllers, EF Core code, or auth logic
- Add new projects instead of restructuring existing ones
- Keep backward compatibility with Part 1
- Follow all causal flows exactly as described
- Do not invent features not listed here

====================================================
TECHNOLOGY DECISIONS (FIXED)
====================================================

- Messaging broker: RabbitMQ
- Messaging framework: NServiceBus
- RabbitMQ is used ONLY as transport
- NServiceBus is the event bus abstraction
- Background jobs implemented using ASP.NET Core BackgroundService
- Database: existing PostgreSQL (reuse existing DB)
- DB schema upgrades via EF Core Migrations
- Docker Compose is used ONLY for infrastructure (Postgres + RabbitMQ)
- API is NOT containerized

====================================================
SOLUTION STRUCTURE REQUIREMENTS
====================================================

Keep existing projects unchanged.

ADD NEW PROJECTS:

1) OrderProcessingSystem.Messaging
   - SDK: Microsoft.NET.Sdk.Worker
   - Hosts:
     - NServiceBus endpoint
     - Event handlers
     - Background (cron-like) services
   - References existing projects where needed (domain / persistence)

2) OrderProcessingSystem.Messaging.Tests
   - SDK: Microsoft.NET.Sdk
   - xUnit
   - Testcontainers for RabbitMQ and PostgreSQL
   - Integration tests for event handling and background jobs

====================================================
EVENT DEFINITIONS
====================================================

Define the following integration events:

- OrderCreated
- OrderCompleted
- OrderExpired

Events contain only necessary data (e.g. OrderId).

====================================================
ORDER CREATION FLOW (CAUSALITY IS CRITICAL)
====================================================

1. User calls POST /api/orders
2. Order is saved to DB with:
   - status = 'pending'
3. OrderCreated event is published
4. API returns response immediately
   - NO waiting
   - NO payment simulation
   - NO status updates beyond 'pending'

====================================================
ORDER PROCESSING (ASYNC EVENT HANDLING)
====================================================

OrderCreatedHandler:
- Runs asynchronously via NServiceBus
- Updates order status:
  pending → processing
- Simulates payment processing:
  - await Task.Delay(5000)
- Randomly decide outcome:
  - 50%:
    - processing → completed
    - publish OrderCompleted event
  - 50%:
    - leave status as 'processing'
    - do NOT publish completion event

====================================================
ORDER EXPIRATION (BACKGROUND JOB)
====================================================

Implement a recurring background job using BackgroundService:

- Runs every 60 seconds
- Finds orders with:
  - status = 'processing'
  - created_at older than 10 minutes
- For each such order:
  - update status to 'expired'
  - publish OrderExpired event

This is NOT an external cron.
This job runs inside the Messaging worker process.

====================================================
NOTIFICATIONS (AUDIT TRAIL)
====================================================

Add a new Notifications table to the database.

Notification entity fields:
- Id (GUID)
- OrderId (GUID)
- Type (Completed / Expired)
- Message (string)
- CreatedAt (UTC timestamp)

Database requirements:
- Create EF Core entity
- Add DbSet
- Create migration as part of DB upgrade mechanism

Notification handling:

OrderCompletedHandler:
- Log fake email notification to console
- Save notification record to database

OrderExpiredHandler:
- Save notification record to database
- No email needed

====================================================
DOCKER COMPOSE REQUIREMENTS
====================================================

Update existing docker-compose.yml:

- Add RabbitMQ service
- Enable RabbitMQ Management UI
- Expose ports for local development
- Reuse existing PostgreSQL service
- Do NOT add API containers

====================================================
VISUAL STUDIO STARTUP (F5 EXPERIENCE)
====================================================

The solution must be runnable via F5 in Visual Studio.

If multiple executable projects exist:
- Configure Visual Studio multi-project startup
- Generate .sln.startup.json with MultiProjectConfigurations
- Pressing F5 must:
  - Start Web API
  - Start Messaging worker
  - Swagger UI must open automatically

====================================================
TESTING REQUIREMENTS
====================================================

Add integration tests in OrderProcessingSystem.Messaging.Tests:

Minimum coverage:
- OrderCreated event handling
- Order status transition to processing
- OrderCompleted event publishing
- Notification persistence
- Order expiration job behavior

Use Testcontainers for:
- PostgreSQL
- RabbitMQ

====================================================
README REQUIREMENTS
====================================================

Update README.md to include:

- Architecture overview of Part 2
- Event-driven flow description
- How to start infrastructure (docker compose)
- How to run DB migrations
- How to start the solution in Visual Studio (F5)
- How background jobs work
- How notifications are handled

## EXAMPLES:

[Provide and explain examples that you have in the `examples/` folder]

## DOCUMENTATION:

Use the official documentation for guidance:
- ASP.NET Core (Web API, JWT, controllers): https://learn.microsoft.com/aspnet/core/?view=aspnetcore-10.0
- EF Core (migrations & PostgreSQL): https://learn.microsoft.com/ef/core/
- RabbitMQ official docs: https://www.rabbitmq.com/docs
- RabbitMQ .NET client API: https://www.rabbitmq.com/client-libraries.html#dotnet-csharp
- NServiceBus core docs: https://docs.particular.net/nservicebus/
- NServiceBus RabbitMQ transport docs: https://docs.particular.net/transports/
- Testcontainers for .NET docs: https://dotnet.testcontainers.org/
- Docker Compose documentation: https://docs.docker.com/compose/
- Swashbuckle / Swagger (OpenAPI) for .NET: https://learn.microsoft.com/aspnet-core/tutorials/getting-started-with-swagger-?view=aspnetcore-10.0

## OTHER CONSIDERATIONS:

- Event handlers must be async
- No blocking calls in controllers
- Controllers must never call handlers directly
- All messaging must go through NServiceBus
- Follow official documentation and best practices
- If a decision is not explicitly described here, ASK before implementing
