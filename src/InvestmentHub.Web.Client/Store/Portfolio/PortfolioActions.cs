using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.Portfolio;

public record LoadPortfoliosAction(string UserId);

public record LoadPortfoliosSuccessAction(List<PortfolioResponseDto> Portfolios);

public record LoadPortfoliosFailureAction(string ErrorMessage);

