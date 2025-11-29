using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Workers;
using InvestmentHub.Workers.Consumers;
using Marten;
using Marten.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Weasel.Core;
using InvestmentHub.Domain.ReadModels;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add database connection (still needed for EF Core if used, but we focus on Marten)
builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres", configureDbContextOptions: options =>
{
    options.UseNpgsql();
});

// Configure Marten
var connectionString = builder.Configuration.GetConnectionString("postgres");
builder.Services.AddMarten((StoreOptions options) =>
{
    options.Connection(connectionString!);
    
    // Use Guid as stream identity (recommended for new projects)
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    
    // Enable Correlation ID in event metadata
    options.Events.MetadataConfig.CorrelationIdEnabled = true;
    
    options.AutoCreateSchemaObjects = AutoCreate.All;
    
    // Force new table name to bypass schema conflicts
    // Force new table name to bypass schema conflicts
    options.Schema.For<PortfolioReadModel>()
        .DocumentAlias("portfolio_read_model_v8")
        .UseNumericRevisions(true);

    options.Schema.For<InvestmentReadModel>()
        .DocumentAlias("investment_read_model_v3")
        .UseNumericRevisions(true);
})
.UseLightweightSessions();

// Explicitly register IDocumentSession as it seems missing in DI
builder.Services.AddScoped<IDocumentSession>(sp => 
    sp.GetRequiredService<IDocumentStore>().LightweightSession());

// Register domain event publisher (Outbox Pattern via Marten)
builder.Services.AddScoped<InvestmentHub.Domain.Common.IDomainEventPublisher, InvestmentHub.Infrastructure.DomainEvents.MartenOutboxDomainEventPublisher>();

// Configure MassTransit
var rabbitMqConnectionString = builder.Configuration["RabbitMQ:ConnectionString"] 
    ?? "amqp://guest:guest@localhost:5672/";

if (!rabbitMqConnectionString.Contains("@") && rabbitMqConnectionString.StartsWith("amqp://"))
{
    rabbitMqConnectionString = rabbitMqConnectionString.Replace("amqp://", "amqp://guest:guest@");
}

builder.Services.AddMassTransit(x =>
{
    // Register consumer
    x.AddConsumer<InvestmentAddedConsumer>();
    x.AddConsumer<InvestmentSoldConsumer>();
    x.AddConsumer<PortfolioCreatedConsumer>();
    x.AddConsumer<InvestmentValueUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqConnectionString);
        
        // Configure global retry policy with Exponential Backoff
        cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));

        // Configure endpoints
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
