var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database - Aspire will manage containers
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// Add Redis cache - Aspire will manage containers
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add Seq for structured logging
var seq = builder.AddContainer("seq", "datalust/seq")
    .WithHttpEndpoint(targetPort: 80, port: 5341, name: "seq")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "true")
    .WithVolume("seq-data", "/data");

// Add Jaeger for distributed tracing
var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one:latest")
    .WithHttpEndpoint(targetPort: 16686, port: 16686, name: "jaeger-ui")
    .WithHttpEndpoint(targetPort: 4317, port: 4317, name: "jaeger-otlp-grpc")
    .WithHttpEndpoint(targetPort: 4318, port: 4318, name: "jaeger-otlp-http")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true");

// Add RabbitMQ for messaging
// Using latest stable version to avoid JavaScript errors in Management UI
var rabbitmq = builder.AddContainer("rabbitmq", "rabbitmq:3.13-management")
    .WithHttpEndpoint(targetPort: 15672, port: 15672, name: "rabbitmq-management")
    .WithEndpoint(5672, 5672, name: "rabbitmq-amqp", scheme: "amqp")
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_VHOST", "/")
    .WithVolume("rabbitmq-data", "/var/lib/rabbitmq");

// Add API service
var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https")
    .WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seq"))
    // Note: We don't set OTEL_EXPORTER_OTLP_ENDPOINT here because:
    // - In Aspire, metrics are automatically exported to Aspire Dashboard
    // - Traces are exported to Jaeger via code configuration (see Extensions.cs)
    // Setting OTEL_EXPORTER_OTLP_ENDPOINT would redirect metrics to Jaeger instead of Dashboard
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", jaeger.GetEndpoint("jaeger-otlp-grpc"))
    // RabbitMQ connection string - using GetEndpoint which Aspire will resolve at runtime
    .WithEnvironment("RabbitMQ__ConnectionString", rabbitmq.GetEndpoint("rabbitmq-amqp"));

// Add Web Client (Blazor WASM)
// Add Web Client (Blazor WASM)
builder.AddProject<Projects.InvestmentHub_Web_Client>("webclient")
    .WithReference(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https")) // Pass API URL to frontend
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Workers service
builder.AddProject<Projects.InvestmentHub_Workers>("workers")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "Workers")
    .WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seq"))
    // Note: We don't set OTEL_EXPORTER_OTLP_ENDPOINT here (see API service comment above)
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", jaeger.GetEndpoint("jaeger-otlp-grpc"))
    // RabbitMQ connection string - using GetEndpoint which Aspire will resolve at runtime
    .WithEnvironment("RabbitMQ__ConnectionString", rabbitmq.GetEndpoint("rabbitmq-amqp"));

await builder.Build().RunAsync();
