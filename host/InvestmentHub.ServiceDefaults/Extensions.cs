using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Configure Serilog first
        builder.ConfigureSerilog();

        // Configure OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // Add default health checks
        builder.AddDefaultHealthChecks();

        // Add service discovery
        builder.Services.AddServiceDiscovery();

        // Configure HTTP client defaults
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Add configuration management
        builder.AddConfigurationManagement();

        // Add dependency injection services
        builder.AddDependencyInjection();

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var aspireDashboardEndpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"];
        // Use JAEGER_OTLP_ENDPOINT instead of OTEL_EXPORTER_OTLP_ENDPOINT to avoid redirecting metrics from Aspire Dashboard
        var jaegerEndpoint = builder.Configuration["JAEGER_OTLP_ENDPOINT"] 
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]; // Fallback for non-Aspire environments
        var isRunningInAspire = !string.IsNullOrWhiteSpace(aspireDashboardEndpoint);

        var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    // Add custom InvestmentHub metrics
                    .AddMeter(InvestmentHubMetrics.MeterName)
                    // Add MassTransit metrics
                    .AddMeter("MassTransit");
                
                // Export metrics to Aspire Dashboard (required for metrics to be visible)
                if (isRunningInAspire && !string.IsNullOrWhiteSpace(aspireDashboardEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(aspireDashboardEndpoint);
                    });
                }
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    // Add MediatR tracing source
                    .AddSource("InvestmentHub.MediatR")
                    // Add Marten tracing source
                    .AddSource("InvestmentHub.Marten")
                    // Add MassTransit tracing source
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
                
                // Export traces to Aspire Dashboard (if in Aspire)
                if (isRunningInAspire && !string.IsNullOrWhiteSpace(aspireDashboardEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(aspireDashboardEndpoint);
                    });
                }
                
                // Export traces to Jaeger (if endpoint is configured)
                if (!string.IsNullOrWhiteSpace(jaegerEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint);
                    });
                }
            });

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(
        this TBuilder builder, 
        OpenTelemetryBuilder openTelemetryBuilder) where TBuilder : IHostApplicationBuilder
    {
        // In Aspire, when running through AppHost, metrics and traces are automatically exported to Aspire Dashboard.
        // The Aspire Dashboard OTLP endpoint is automatically configured via environment variables.
        // We only need to configure OTLP exporter if we want to export to an external endpoint (e.g., Jaeger).
        
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var aspireDashboardEndpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"];
        
        // If running in Aspire (ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL is set), metrics are automatically exported to Dashboard.
        // We don't need to call UseOtlpExporter() - Aspire handles it automatically.
        // However, if OTEL_EXPORTER_OTLP_ENDPOINT is also set (for Jaeger), we can export to both.
        
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            // Export to external OTLP endpoint (e.g., Jaeger)
            // Note: In Aspire, metrics will still go to Dashboard automatically
            // UseOtlpExporter() will use the endpoint from OTEL_EXPORTER_OTLP_ENDPOINT environment variable
            // and default to gRPC protocol
            openTelemetryBuilder.UseOtlpExporter();
        }
        // If not in Aspire and no OTLP endpoint is set, OpenTelemetry will use default configuration
        // (which may not export anywhere, but that's fine for local development)

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    openTelemetryBuilder.UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    /// <summary>
    /// Configures Serilog logging with structured logging, enrichers, and multiple sinks
    /// </summary>
    public static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            // Add OpenTelemetry TraceId and SpanId to all log entries
            // This enables correlation between logs (Seq) and traces (Jaeger)
            .Enrich.With<ActivityTraceEnricher>()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/investmenthub-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.OpenTelemetry(options =>
            {
                var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                }
            });

        // Add Seq sink conditionally with enhanced configuration for structured logging
        var seqUrl = builder.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrEmpty(seqUrl))
        {
            loggerConfig = loggerConfig.WriteTo.Seq(
                serverUrl: seqUrl,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                batchPostingLimit: 1000, // Batch size for better performance
                period: TimeSpan.FromSeconds(2), // Flush interval
                eventBodyLimitBytes: 256 * 1024 // 256KB max event size
            );
        }

        Log.Logger = loggerConfig.CreateLogger();

        // Clear existing providers and use Serilog
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        return builder;
    }

    /// <summary>
    /// Adds configuration management with user secrets support
    /// </summary>
    public static TBuilder AddConfigurationManagement<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Add user secrets in development
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets(typeof(TBuilder).Assembly);
        }

        // Add environment variables
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    /// <summary>
    /// Adds common dependency injection services
    /// </summary>
    public static TBuilder AddDependencyInjection<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Add options pattern
        builder.Services.AddOptions();

        // Add configuration binding
        builder.Services.Configure<ApplicationOptions>(builder.Configuration.GetSection("Application"));

        // Add HTTP client factory
        builder.Services.AddHttpClient();

        // Add memory cache
        builder.Services.AddMemoryCache();

        // Add Redis cache (if Redis connection string is available)
        var redisConnectionString = builder.Configuration.GetConnectionString("redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
        }

        // Add response compression
        // builder.Services.AddResponseCompression();

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader();
            });
        });

        return builder;
    }
}

/// <summary>
/// Application configuration options
/// </summary>
public class ApplicationOptions
{
    public string Name { get; set; } = "InvestmentHub";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSwagger { get; set; } = true;
}
