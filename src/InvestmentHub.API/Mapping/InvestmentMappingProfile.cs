using AutoMapper;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.API.Controllers;
using InvestmentHub.API.DTOs;
using InvestmentHub.Domain.Repositories;

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
            .ForMember(dest => dest.PortfolioId, opt => opt.MapFrom(src => PortfolioId.FromString(src.PortfolioId)))
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => new Symbol(
                src.Symbol.Ticker,
                src.Symbol.Exchange,
                Enum.Parse<AssetType>(src.Symbol.AssetType))))
            .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => new Money(
                src.PurchasePrice.Amount,
                Enum.Parse<Currency>(src.PurchasePrice.Currency))));

        CreateMap<UpdateInvestmentValueRequest, UpdateInvestmentValueCommand>()
            .ForMember(dest => dest.InvestmentId, opt => opt.MapFrom(src => InvestmentId.FromString(src.InvestmentId)))
            .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => new Money(
                src.CurrentPrice.Amount,
                Enum.Parse<Currency>(src.CurrentPrice.Currency))));

        CreateMap<CreatePortfolioRequest, CreatePortfolioCommand>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => UserId.FromString(src.OwnerId)));

        // Domain entities to Response DTOs
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

        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value.ToString()));
    }
}
