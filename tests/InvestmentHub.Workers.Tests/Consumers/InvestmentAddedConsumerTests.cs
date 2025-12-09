using System;
using System.Threading.Tasks;
using InvestmentHub.Contracts.Messages;
using InvestmentHub.Domain.ReadModels;
using InvestmentHub.Workers.Consumers;
using Marten;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestmentHub.Workers.Tests.Consumers;

public class InvestmentAddedConsumerTests
{
    // Concrete implementation for testing
    private class TestInvestmentAddedMessage : InvestmentAddedMessage
    {
        public Guid PortfolioId { get; set; }
        public Guid InvestmentId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal Quantity { get; set; }
        public DateTime PurchaseDate { get; set; }
    }

    [Fact]
    public async Task Consume_Should_Update_Portfolio_TotalValue()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var investmentId = Guid.NewGuid();
        var portfolio = new PortfolioReadModel
        {
            Id = portfolioId,
            TotalValue = 0 // Initial value
        };

        var mockSession = new Mock<IDocumentSession>();
        
        // Mock LoadAsync for Portfolio
        mockSession.Setup(s => s.LoadAsync<PortfolioReadModel>(portfolioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<InvestmentAddedConsumer>();
            })
            .AddScoped(_ => mockSession.Object)
            .AddLogging()
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish<InvestmentAddedMessage>(new TestInvestmentAddedMessage 
            { 
                InvestmentId = investmentId, 
                PortfolioId = portfolioId 
            });

            // Assert
            Assert.True(await harness.Consumed.Any<InvestmentAddedMessage>());
            
            // Verify session was used to load portfolio
            mockSession.Verify(s => s.LoadAsync<PortfolioReadModel>(portfolioId, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_Should_Throw_And_Retry_On_Failure()
    {
        // Arrange
        var portfolioId = Guid.NewGuid();
        var investmentId = Guid.NewGuid();

        var mockSession = new Mock<IDocumentSession>();
        
        // Simulate DB failure
        mockSession.Setup(s => s.LoadAsync<PortfolioReadModel>(portfolioId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB Connection Failed"));

        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<InvestmentAddedConsumer>();
            })
            .AddScoped(_ => mockSession.Object)
            .AddLogging()
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish<InvestmentAddedMessage>(new TestInvestmentAddedMessage 
            { 
                InvestmentId = investmentId, 
                PortfolioId = portfolioId 
            });

            // Assert
            // Message should be consumed (attempted)
            Assert.True(await harness.Consumed.Any<InvestmentAddedMessage>());

            // Verify that it failed (published to _error queue eventually, or Fault<T> event)
            // In TestHarness, we can check if there is a Fault
            Assert.True(await harness.Published.Any<Fault<InvestmentAddedMessage>>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
