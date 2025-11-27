using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.Data.Repositories;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Services;
using Microsoft.EntityFrameworkCore;
using MediatR;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.Handlers.Queries;
using InvestmentHub.Domain.Behaviors;
using InvestmentHub.Domain.Validators;
using InvestmentHub.API.Mapping;
using Marten;
using Marten.Events;
using InvestmentHub.Domain.Projections;
using InvestmentHub.API.Middleware;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add database connection
builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres", configureDbContextOptions: options =>
{
    options.UseNpgsql();
});

// Configure Marten for Event Sourcing
var connectionString = builder.Configuration.GetConnectionString("postgres");
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString!);
    
    // Use Guid as stream identity (recommended for new projects)
    options.Events.StreamIdentity = StreamIdentity.AsGuid;
    
    // Enable Correlation ID in event metadata
    // This allows Marten to store Correlation ID with each event
    options.Events.MetadataConfig.CorrelationIdEnabled = true;
    
    // Register projections
    options.Projections.Add<PortfolioProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
    options.Projections.Add<InvestmentProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
    
    // Configure database schema (optional - Marten will auto-create if needed)
    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
    }
});

// Register HTTP context accessor for Correlation ID enrichment
builder.Services.AddHttpContextAccessor();

// Register Marten Correlation ID enricher
builder.Services.AddScoped<InvestmentHub.Domain.Services.ICorrelationIdEnricher, InvestmentHub.Infrastructure.Marten.MartenCorrelationIdEnricher>();

// Register metrics recorder
builder.Services.AddScoped<InvestmentHub.Domain.Services.IMetricsRecorder, InvestmentHub.Infrastructure.Metrics.MetricsRecorder>();

// Register repositories
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register domain services
builder.Services.AddScoped<IPortfolioValuationService, PortfolioValuationService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InvestmentHub API", Version = "v1" });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(InvestmentMappingProfile));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssembly(typeof(AddInvestmentCommandValidator).Assembly);

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    // Register all handlers from the Domain assembly
    cfg.RegisterServicesFromAssembly(typeof(AddInvestmentCommandHandler).Assembly);
    
    // Register pipeline behaviors
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Add MassTransit for messaging
var rabbitMqConnectionString = builder.Configuration["RabbitMQ:ConnectionString"] 
    ?? "amqp://guest:guest@localhost:5672/";

// If Aspire endpoint doesn't include credentials, add them
if (!rabbitMqConnectionString.Contains("@") && rabbitMqConnectionString.StartsWith("amqp://"))
{
    rabbitMqConnectionString = rabbitMqConnectionString.Replace("amqp://", "amqp://guest:guest@");
}

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        // Simple configuration - MassTransit will parse the connection string
        cfg.Host(rabbitMqConnectionString);
        
        // Configure endpoints (consumers will be added in later steps)
        cfg.ConfigureEndpoints(context);
    });
});

// Note: MassTransit hosted service is automatically registered in MassTransit 8.x
// No need to call AddMassTransitHostedService() - it's obsolete

var app = builder.Build();

// Log Seq configuration status
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var seqUrl = app.Configuration["Seq:ServerUrl"];
if (!string.IsNullOrEmpty(seqUrl))
{
    logger.LogInformation("Seq logging configured and enabled. Seq Server URL: {SeqUrl}", seqUrl);
    logger.LogInformation("Structured logging to Seq is active. Check Seq UI at {SeqUrl} to view logs.", seqUrl);
}
else
{
    logger.LogWarning("Seq logging is not configured. Set 'Seq:ServerUrl' in configuration to enable Seq.");
}

// Log RabbitMQ configuration status
var rabbitMqUrl = app.Configuration["RabbitMQ:ConnectionString"];
if (!string.IsNullOrEmpty(rabbitMqUrl))
{
    logger.LogInformation("RabbitMQ messaging configured and enabled. RabbitMQ Connection: {RabbitMqUrl}", rabbitMqUrl);
    logger.LogInformation("MassTransit is active. RabbitMQ Management UI available at http://localhost:15672 (guest/guest)");
}
else
{
    logger.LogWarning("RabbitMQ messaging is not configured. Set 'RabbitMQ:ConnectionString' in configuration to enable messaging.");
}

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Correlation ID middleware first (before other middleware)
// This ensures Correlation ID is available in all subsequent logs
app.UseMiddleware<CorrelationIdMiddleware>();

// Add other middleware
app.UseCors("AllowAll");
app.UseResponseCompression();

// Add global exception handler before mapping endpoints
// This catches all exceptions from controllers and other middleware
app.UseMiddleware<GlobalExceptionHandler>();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();