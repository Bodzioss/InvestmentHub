using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InvestmentHub.Contracts;
using InvestmentHub.Contracts.Auth;
using InvestmentHub.Contracts.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace InvestmentHub.E2E.Tests;

public class AuthenticationFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitmq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();

    private WebApplicationFactory<Program> _apiFactory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitmq.StartAsync();

        _apiFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Workaround for .NET 9 TestServer PipeWriter issue:
                    // Use Newtonsoft.Json instead of System.Text.Json to avoid UnflushedBytes error
                    services.AddControllers()
                        .AddNewtonsoftJson();
                });
                
                builder.UseSetting("ConnectionStrings:postgres", _postgres.GetConnectionString());
                builder.UseSetting("RabbitMQ:ConnectionString", _rabbitmq.GetConnectionString());
            });

        _client = _apiFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitmq.DisposeAsync();
        await _apiFactory.DisposeAsync();
    }

    [Fact]
    public async Task Should_Register_New_User_Successfully()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = $"test-{Guid.NewGuid()}@example.com",
            Password = "Test123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(registerRequest.Email);
        authResponse.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Login_With_Valid_Credentials()
    {
        // Arrange - First register a user
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test123!";
        var name = "Test User";
        
        var registerRequest = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(email);
        authResponse.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Reject_Login_With_Invalid_Credentials()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Change_Password_With_Valid_Current_Password()
    {
        // Arrange - Register and login
        var email = $"test-{Guid.NewGuid()}@example.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword456!";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = email,
            Password = oldPassword
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        // Set authorization header
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify we can login with new password
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = newPassword
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Current_Password_When_Changing()
    {
        // Arrange - Register and login
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        
        var registerRequest = new RegisterRequest
        {
            Name = "Test User",
            Email = email,
            Password = password
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Update_User_Profile_Successfully()
    {
        // Arrange - Register a user
        var email = $"test-{Guid.NewGuid()}@example.com";
        var originalName = "Original Name";
        var updatedName = "Updated Name";
        
        var registerRequest = new RegisterRequest
        {
            Name = originalName,
            Email = email,
            Password = "Test123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        // Extract user ID from token (we need to parse JWT or get it from API)
        // For simplicity, let's get all users and find ours
        var usersResponse = await _client.GetAsync("/api/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserResponseDto>>();
        var user = users!.First(u => u.Email == email);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateUserRequest
        {
            Name = updatedName
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/users/{user.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getUserResponse = await _client.GetAsync($"/api/users/{user.Id}");
        var updatedUser = await getUserResponse.Content.ReadFromJsonAsync<UserResponseDto>();
        updatedUser!.Name.Should().Be(updatedName);
    }

    [Fact]
    public async Task JWT_Token_Should_Contain_User_Name_Claim()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid()}@example.com";
        var name = "Test User Name";
        
        var registerRequest = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = "Test123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert - Parse JWT and verify it contains name claim
        var token = authResponse!.Token;
        token.Should().NotBeNullOrEmpty();

        // Simple JWT parsing (split by dots and decode base64)
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        var payload = parts[1];
        // Pad base64 if needed
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var payloadBytes = Convert.FromBase64String(payload);
        var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
        
        // Verify the payload contains the name
        payloadJson.Should().Contain(name);
    }
}
