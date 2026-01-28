using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using InvestmentHub.API.Extensions;
using InvestmentHub.API.Mapping;
using InvestmentHub.API.Hubs;
using InvestmentHub.API.Consumers;
using InvestmentHub.API.Middleware;
using InvestmentHub.Domain.Behaviors;
using InvestmentHub.Domain.Handlers.Commands;
using InvestmentHub.Domain.Interfaces;
using InvestmentHub.Domain.Projections;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Services;
using InvestmentHub.Domain.Validators;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.Data.Repositories;
using InvestmentHub.Infrastructure.Extensions;
using InvestmentHub.Infrastructure.MarketData;
using InvestmentHub.Infrastructure.Projections;
using Marten;
using Marten.Events;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using YahooQuotesApi;

var builder = WebApplication.CreateBuilder(args);

// Enable legacy timestamp behavior for Npgsql/Marten compatibility with DateTimeKind.Utc
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.AddServiceDefaults();

// Add database connection with pgvector support
builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres", configureDbContextOptions: options =>
{
    options.UseNpgsql(npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
    // Suppress pending model changes warning - we use manual SQL migrations for pgvector tables
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Configure Marten for Event Sourcing
var connectionString = builder.Configuration.GetConnectionString("postgres");
builder.Services.AddMarten(sp =>
{
    var options = new StoreOptions();
    options.Connection(connectionString!);

    // Use Guid as stream identity (recommended for new projects)
    options.Events.StreamIdentity = StreamIdentity.AsGuid;

    // Enable Correlation ID in event metadata
    options.Events.MetadataConfig.CorrelationIdEnabled = true;

    // NOTE: Inline projections removed for pure CQRS pattern
    // Read models are now updated ONLY by consumers (InvestmentAddedConsumer, etc.)
    // This ensures:
    // 1. True command/query separation
    // 2. No race conditions between inline and async updates
    // 3. Faster API response times (no projection wait)
    // 4. All read model updates go through RabbitMQ -> Workers

    // Configure database schema (optional - Marten will auto-create if needed)
    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
    }

    // Force new table name to bypass schema conflicts
    options.Schema.For<PortfolioReadModel>()
        .DocumentAlias("portfolio_read_model_v8")
        .UseNumericRevisions(true);

    options.Schema.For<InvestmentReadModel>()
        .DocumentAlias("investment_read_model_v3")
        .UseNumericRevisions(true);

    // Register MassTransitOutboxProjection as ASYNC projection
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    options.Projections.Add(new MassTransitOutboxProjection(scopeFactory), ProjectionLifecycle.Async);
    options.Projections.Add<InvestmentProjection>(ProjectionLifecycle.Inline);
    options.Projections.Add<PortfolioProjection>(ProjectionLifecycle.Inline);
    return options;
})
.UseLightweightSessions()
.AddAsyncDaemon(DaemonMode.Solo);

// Explicitly register IDocumentSession as it seems missing in DI
builder.Services.AddScoped<IDocumentSession>(sp =>
    sp.GetRequiredService<IDocumentStore>().LightweightSession());

// Register HTTP context accessor for Correlation ID enrichment
builder.Services.AddHttpContextAccessor();

// Register Marten Correlation ID enricher
builder.Services.AddScoped<InvestmentHub.Domain.Services.ICorrelationIdEnricher, InvestmentHub.Infrastructure.Marten.MartenCorrelationIdEnricher>();
// Register domain event publisher (Outbox Pattern via Marten)
builder.Services.AddScoped<InvestmentHub.Domain.Common.IDomainEventPublisher, InvestmentHub.Infrastructure.DomainEvents.MartenOutboxDomainEventPublisher>();

// Register metrics recorder
builder.Services.AddScoped<InvestmentHub.Domain.Services.IMetricsRecorder, InvestmentHub.Infrastructure.Metrics.MetricsRecorder>();

// Register repositories
builder.Services.AddScoped<IInvestmentRepository, InvestmentRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Register domain services
// Register domain services
builder.Services.AddScoped<IPortfolioValuationService, PortfolioValuationService>();
builder.Services.AddScoped<IPortfolioHistoryService, InvestmentHub.Infrastructure.Services.PortfolioHistoryService>();

// Add resilience services
builder.Services.AddResilienceServices();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.SetIsOriginAllowed(_ => true) // Allow any origin, robust matching
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

// Add API services

// Add API services
builder.Services.AddControllers();
builder.Services.AddSignalR();
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

// Add Problem Details and Exception Handler
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    // Register all handlers from the Domain assembly
    cfg.RegisterServicesFromAssembly(typeof(AddInvestmentCommandHandler).Assembly);

    // Register all handlers from the Infrastructure assembly (transaction handlers)
    cfg.RegisterServicesFromAssembly(typeof(InvestmentHub.Infrastructure.Handlers.Queries.GetTransactionsQueryHandler).Assembly);

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
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Simple configuration - MassTransit will parse the connection string
        cfg.Host(rabbitMqConnectionString);

        // Configure global retry policy with Exponential Backoff
        cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)));

        // Configure endpoints (consumers will be added in later steps)
        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks for infrastructure dependencies
builder.Services.AddInfrastructureHealthChecks(builder.Configuration, builder.Environment);

// Register YahooQuotes service
builder.Services.AddSingleton(new YahooQuotesBuilder().Build());

// Add Market Data Providers
builder.Services.AddScoped<IMarketDataProvider, StooqMarketDataProvider>();
builder.Services.AddScoped<IMarketDataProvider, YahooMarketDataProvider>();
builder.Services.AddScoped<IMarketPriceRepository, InvestmentHub.Infrastructure.Repositories.MarketPriceRepository>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.Services.MarketPriceService>();
builder.Services.AddScoped<IExchangeRateService, InvestmentHub.Infrastructure.Services.ExchangeRateService>();

// Add AI Services
builder.Services.AddScoped<InvestmentHub.Infrastructure.AI.IGeminiService, InvestmentHub.Infrastructure.AI.GeminiService>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.AI.DocumentProcessor>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.AI.VectorSearchService>();

// Add Treasury Bonds Services
builder.Services.AddScoped<InvestmentHub.Infrastructure.TreasuryBonds.BondValueCalculator>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.TreasuryBonds.BondDataProvider>();

// Add Background Jobs
builder.Services.AddScoped<InvestmentHub.Infrastructure.Jobs.PriceUpdateJob>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.Jobs.HistoricalImportJob>();
builder.Services.AddScoped<InvestmentHub.Infrastructure.Data.InstrumentImporter>();

// Add CSV Import Services
builder.Services.AddScoped<InvestmentHub.Infrastructure.Services.MyFundCsvParser>();

// Add Hangfire services
builder.Services.AddHangfireServices(builder.Configuration);

// Add Identity
builder.Services.AddIdentityServices();

// Add Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PortfolioOwner", policy =>
        policy.Requirements.Add(new InvestmentHub.API.Authorization.PortfolioOwnerRequirement()));
});

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, InvestmentHub.API.Authorization.PortfolioOwnerHandler>();
builder.Services.AddScoped<InvestmentHub.API.Services.TokenService>();
builder.Services.AddScoped<INotificationService, InvestmentHub.API.Services.SignalRNotificationService>();

// Configure Forwarded Headers for Azure Container Apps (Reverse Proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Trust all networks - Azure Load Balancer can come from any internal IP
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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
    // Apply pending migrations first (only if needed)
    var dbContext = scope.ServiceProvider.GetRequiredService<InvestmentHub.Infrastructure.Data.ApplicationDbContext>();
    
    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations to apply");
        }
    }
    catch (Exception ex) when (ex.InnerException?.Message.Contains("already exists") == true)
    {
        logger.LogWarning("Migration skipped - tables already exist: {Message}", ex.Message);
        // Ensure EnsureCreated is not called if tables exist - just continue
    }

    // Use ServiceProvider to allow Seeder to create background scopes
    var yahooQuotes = scope.ServiceProvider.GetRequiredService<YahooQuotesApi.YahooQuotes>();
    var historicalImportJob = scope.ServiceProvider.GetRequiredService<InvestmentHub.Infrastructure.Jobs.HistoricalImportJob>();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider, yahooQuotes, historicalImportJob);
}

// Configure the HTTP request pipeline.

// Process Forwarded Headers FIRST so we know the real protocol (HTTPS) and IP
app.UseForwardedHeaders();

// Global exception handler
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    // Only use forced HTTPS redirection in Development
    // In Production/Container Apps, TLS is terminated by Ingress, so we receive HTTP
    app.UseHttpsRedirection();
}

// Explicitly add UseRouting before CORS
app.UseRouting();

// CORS MUST be used between UseRouting and UseEndpoints (MapControllers etc)
app.UseCors("AllowAll");

// Add Correlation ID middleware after CORS
// This ensures Correlation ID is available in all subsequent logs
app.UseMiddleware<CorrelationIdMiddleware>();

// Add Request Timing Middleware to expose backend latency via Server-Timing header
app.UseMiddleware<RequestTimingMiddleware>();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/healthchecks-ui";
});

// Map health check endpoint with JSON response for UI compatibility
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Use Hangfire Dashboard
app.UseHangfireDashboard();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}