using AutoMapper;
using Sapphire.Billing.Application.DTOs;
using Sapphire.Billing.Domain.Aggregates;

namespace Sapphire.Billing.Application.Mappings;

/// <summary>
/// AutoMapper profile for Billing aggregate mappings.
/// </summary>
public sealed class BillingMappingProfile : Profile
{
    public BillingMappingProfile()
    {
        CreateMap<Tariff, TariffDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<Wallet, WalletDto>();
    }
}
