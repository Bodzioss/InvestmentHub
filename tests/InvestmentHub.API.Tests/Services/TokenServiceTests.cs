using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using InvestmentHub.API.Services;
using InvestmentHub.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InvestmentHub.API.Tests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "ThisIsAVerySecureKeyForTestingPurposesOnly12345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void CreateToken_Should_Generate_Valid_JWT()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        // Act
        var token = _tokenService.CreateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void CreateToken_Should_Include_Name_Claim_When_Provided()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com"
        };
        var userName = "Test User";

        // Act
        var token = _tokenService.CreateToken(user, userName);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == userName);
    }

    [Fact]
    public void CreateToken_Should_Fallback_To_Email_When_Name_Not_Provided()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        // Act
        var token = _tokenService.CreateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Email);
    }

    [Fact]
    public void CreateToken_Should_Set_Expiration_To_7_Days()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        // Act
        var token = _tokenService.CreateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var expectedExpiration = DateTime.UtcNow.AddDays(7);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
    }
}
