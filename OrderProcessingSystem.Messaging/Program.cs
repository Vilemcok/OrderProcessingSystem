using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Messaging.BackgroundJobs;

var host = Host.CreateDefaultBuilder(args)
    .UseNServiceBus(context =>
    {
        var endpointConfiguration = new EndpointConfiguration("OrderProcessingSystem.Messaging");

        // Configure RabbitMQ transport
        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        var connectionString = context.Configuration.GetConnectionString("RabbitMQ")
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
    })
    .ConfigureServices((context, services) =>
    {
        // Add DbContext with same connection string as WebApi
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // Register background services
        services.AddHostedService<OrderExpirationBackgroundService>();
    })
    .Build();

await host.RunAsync();
