var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// Add Redis cache
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add API service
var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Web service
var web = builder.AddProject<Projects.InvestmentHub_Web>("web")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(api)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Workers service
var workers = builder.AddProject<Projects.InvestmentHub_Workers>("workers")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "Workers");

builder.Build().Run();
