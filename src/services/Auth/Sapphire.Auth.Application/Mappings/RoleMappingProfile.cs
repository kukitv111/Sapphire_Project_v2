using AutoMapper;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Application.Mappings;

/// <summary>
/// AutoMapper profile for Role entity mappings.
/// </summary>
public sealed class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.IsSystem, opt => opt.MapFrom(src => src.IsSystem));

        CreateMap<RefreshToken, RefreshTokenDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))
            .ForMember(dest => dest.IsRevoked, opt => opt.MapFrom(src => src.IsRevoked))
            .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => src.DeviceInfo));
    }
}
