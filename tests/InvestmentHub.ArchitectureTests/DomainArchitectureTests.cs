using FluentAssertions;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Infrastructure.Data;
using NetArchTest.Rules;
using Xunit;

namespace InvestmentHub.ArchitectureTests;

public class DomainArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        // Assemble
        var domainAssembly = typeof(Portfolio).Assembly;
        var infrastructureAssembly = typeof(ApplicationDbContext).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(infrastructureAssembly.GetName().Name)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain layer should not depend on Infrastructure layer");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_API()
    {
        // Assemble
        var domainAssembly = typeof(Portfolio).Assembly;
        
        // Act
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("InvestmentHub.API")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Domain layer should not depend on API layer");
    }
}
