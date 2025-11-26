using InvestmentHub.Contracts;

namespace InvestmentHub.Web.Client.Store.Investment;

/// <summary>
/// Action to load investments for a portfolio
/// </summary>
public record LoadInvestmentsAction(string PortfolioId);

/// <summary>
/// Action dispatched when investments are successfully loaded
/// </summary>
public record LoadInvestmentsSuccessAction(string PortfolioId, List<InvestmentResponseDto> Investments);

/// <summary>
/// Action dispatched when loading investments fails
/// </summary>
public record LoadInvestmentsFailureAction(string ErrorMessage);

/// <summary>
/// Action to add a new investment
/// </summary>
public record AddInvestmentAction(AddInvestmentRequest Request);

/// <summary>
/// Action to update an investment
/// </summary>
public record UpdateInvestmentValueAction(string InvestmentId, UpdateInvestmentValueRequest Request);

/// <summary>
/// Action to delete an investment
/// </summary>
public record DeleteInvestmentAction(string InvestmentId);

/// <summary>
/// Action to sell an investment
/// </summary>
public record SellInvestmentAction(string InvestmentId, SellInvestmentRequest Request);

