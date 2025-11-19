var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database - Aspire will manage containers
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// Add Redis cache - Aspire will manage containers
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add API service
var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Web Client (Blazor WASM)
var webClient = builder.AddProject<Projects.InvestmentHub_Web_Client>("webclient")
    .WithReference(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https")) // Pass API URL to frontend
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Workers service
var workers = builder.AddProject<Projects.InvestmentHub_Workers>("workers")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "Workers");

builder.Build().Run();
