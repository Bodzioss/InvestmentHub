using FluentAssertions;
using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.ValueObjects;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvestmentHub.Domain.Tests.Integration;

public class MartenDeleteTest : IClassFixture<MartenFixture>
{
    private readonly MartenFixture _fixture;

    public MartenDeleteTest(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Load_And_Delete_Portfolio()
    {
        // Arrange
        await using var session = _fixture.Store?.LightweightSession();
        var portfolioId = PortfolioId.New();
        var ownerId = UserId.New();
        
        // Create
        var aggregate = PortfolioAggregate.Initiate(portfolioId, ownerId, "Delete Test Portfolio", "Description");
        session!.Events.StartStream<PortfolioAggregate>(portfolioId.Value, aggregate.GetUncommittedEvents().ToArray());
        await session.SaveChangesAsync();

        // Act - Load
        var loadedAggregate = await session.Events.AggregateStreamAsync<PortfolioAggregate>(portfolioId.Value);

        // Assert - Load
        loadedAggregate.Should().NotBeNull();
        loadedAggregate!.Id.Should().Be(portfolioId.Value);
        loadedAggregate.Name.Should().Be("Delete Test Portfolio");

        // Act - Delete (Close)
        var closeEvent = loadedAggregate.Close("Deleted test", ownerId);
        session.Events.Append(portfolioId.Value, closeEvent);
        await session.SaveChangesAsync();
        
        // Assert - Verify Closed
        var reloadedAggregate = await session.Events.AggregateStreamAsync<PortfolioAggregate>(portfolioId.Value);
        reloadedAggregate!.IsClosed.Should().BeTrue();
    }
}
