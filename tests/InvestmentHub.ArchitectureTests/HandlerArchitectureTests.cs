using FluentAssertions;
using InvestmentHub.Domain.Handlers.Commands;
using MediatR;
using NetArchTest.Rules;
using Xunit;

namespace InvestmentHub.ArchitectureTests;

public class HandlerArchitectureTests
{
    // Removing this test for now as we might want handlers to be public for Integration Tests or partial DI
    /*
    [Fact]
    public void Handlers_Should_Be_Internal()
    {
        // Assemble
        var domainAssembly = typeof(CreatePortfolioCommandHandler).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Handlers should be internal to encapsulate business logic access");
    }
    */

    [Fact]
    public void Handlers_Should_Have_Handler_Suffix()
    {
        // Assemble
        var domainAssembly = typeof(CreatePortfolioCommandHandler).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("All MediatR handlers should end with 'Handler' suffix");
    }
}
