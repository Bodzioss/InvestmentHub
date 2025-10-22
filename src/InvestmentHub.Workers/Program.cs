using InvestmentHub.Workers;
using Microsoft.EntityFrameworkCore;
using InvestmentHub.Web.Data;

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
