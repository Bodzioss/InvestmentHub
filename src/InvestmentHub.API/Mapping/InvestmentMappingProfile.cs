using AutoMapper;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.API.Controllers;
using InvestmentHub.Contracts;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.Queries;

namespace InvestmentHub.API.Mapping;

/// <summary>
/// AutoMapper profile for mapping between DTOs and domain models.
/// </summary>
public class InvestmentMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the InvestmentMappingProfile class.
    /// </summary>
    public InvestmentMappingProfile()
    {
        // Request DTOs to Commands
        CreateMap<AddInvestmentRequest, AddInvestmentCommand>()
            .ConstructUsing(src => new AddInvestmentCommand(
                PortfolioId.FromString(src.PortfolioId),
                new Symbol(
                    src.Symbol.Ticker,
                    src.Symbol.Exchange,
                    Enum.Parse<AssetType>(src.Symbol.AssetType)),
                new Money(
                    src.PurchasePrice.Amount,
                    Enum.Parse<Currency>(src.PurchasePrice.Currency)),
                src.Quantity,
                src.PurchaseDate))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<UpdateInvestmentValueRequest, UpdateInvestmentValueCommand>()
            .ConstructUsing(src => new UpdateInvestmentValueCommand(
                InvestmentId.FromString(src.InvestmentId),
                new Money(
                    src.CurrentPrice.Amount,
                    Enum.Parse<Currency>(src.CurrentPrice.Currency))))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<SellInvestmentRequest, SellInvestmentCommand>()
            .ConstructUsing(src => new SellInvestmentCommand(
                InvestmentId.FromString(src.InvestmentId),
                new Money(
                    src.SalePrice.Amount,
                    Enum.Parse<Currency>(src.SalePrice.Currency)),
                src.QuantityToSell,
                src.SaleDate))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<CreatePortfolioRequest, CreatePortfolioCommand>()
            .ConstructUsing(src => new CreatePortfolioCommand(
                PortfolioId.New(),
                UserId.FromString(src.OwnerId),
                src.Name,
                src.Description,
                src.Currency))
            .ForAllMembers(opt => opt.Ignore());

        // Domain entities to Response DTOs
        CreateMap<InvestmentSummary, InvestmentResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value.ToString()))
            .ForMember(dest => dest.PortfolioId, opt => opt.Ignore()) // Will be set in controller
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => new SymbolResponseDto
            {
                Ticker = src.Symbol.Ticker,
                Exchange = src.Symbol.Exchange,
                AssetType = src.Symbol.AssetType.ToString()
            }))
            .ForMember(dest => dest.CurrentValue, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.CurrentValue.Amount,
                Currency = src.CurrentValue.Currency.ToString()
            }))
            .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.PurchasePrice.Amount,
                Currency = src.PurchasePrice.Currency.ToString()
            }))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.PurchaseDate, opt => opt.MapFrom(src => src.PurchaseDate))
            .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.LastUpdated))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Investment, InvestmentResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value.ToString()))
            .ForMember(dest => dest.PortfolioId, opt => opt.MapFrom(src => src.PortfolioId.Value.ToString()))
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => new SymbolResponseDto
            {
                Ticker = src.Symbol.Ticker,
                Exchange = src.Symbol.Exchange,
                AssetType = src.Symbol.AssetType.ToString()
            }))
            .ForMember(dest => dest.CurrentValue, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.CurrentValue.Amount,
                Currency = src.CurrentValue.Currency.ToString()
            }))
            .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.PurchasePrice.Amount,
                Currency = src.PurchasePrice.Currency.ToString()
            }))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Portfolio, PortfolioResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value.ToString()))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId.Value.ToString()))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.GetTotalValue().Amount,
                Currency = src.GetTotalValue().Currency.ToString()
            }))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.GetTotalCost().Amount,
                Currency = src.GetTotalCost().Currency.ToString()
            }))
            .ForMember(dest => dest.UnrealizedGainLoss, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.GetTotalGainLoss().Amount,
                Currency = src.GetTotalGainLoss().Currency.ToString()
            }))
            .ForMember(dest => dest.ActiveInvestmentCount, opt => opt.MapFrom(src => src.Investments.Count));

        // Map PortfolioReadModel to PortfolioResponseDto (for queries from read model)
        CreateMap<InvestmentHub.Domain.ReadModels.PortfolioReadModel, PortfolioResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId.ToString()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.LastUpdated))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.TotalValue,
                Currency = src.Currency
            }))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = src.TotalCost,
                Currency = src.Currency
            }))
            .ForMember(dest => dest.UnrealizedGainLoss, opt => opt.MapFrom(src => new MoneyResponseDto
            {
                Amount = 0, // Not yet calculated in read model
                Currency = src.Currency
            }))
            .ForMember(dest => dest.ActiveInvestmentCount, opt => opt.MapFrom(src => src.InvestmentCount));

        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value.ToString()));
    }
}
