using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Add database connection
builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres", configureDbContextOptions: options =>
{
    options.UseNpgsql();
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
