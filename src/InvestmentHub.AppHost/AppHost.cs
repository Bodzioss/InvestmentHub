var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.InvestmentHub>("investmenthub");

builder.Build().Run();
