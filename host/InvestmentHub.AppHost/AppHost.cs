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

// Add API service
var api = builder.AddProject<Projects.InvestmentHub_API>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https")
    .WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seq"));

// Add Web Client (Blazor WASM)
var webClient = builder.AddProject<Projects.InvestmentHub_Web_Client>("webclient")
    .WithReference(api)
    .WithEnvironment("ApiSettings__BaseUrl", api.GetEndpoint("https")) // Pass API URL to frontend
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "https");

// Add Workers service
var workers = builder.AddProject<Projects.InvestmentHub_Workers>("workers")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "Workers")
    .WithEnvironment("Seq__ServerUrl", seq.GetEndpoint("seq"));

builder.Build().Run();
