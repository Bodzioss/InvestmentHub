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
    
    // Register projections
    options.Projections.Add<PortfolioProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
    options.Projections.Add<InvestmentProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
    
    // Configure database schema (optional - Marten will auto-create if needed)
    if (builder.Environment.IsDevelopment())
    {
        options.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
    }
});

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

var app = builder.Build();

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

// Add middleware
app.UseCors("AllowAll");
app.UseResponseCompression();

// Map default endpoints (health checks)
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
