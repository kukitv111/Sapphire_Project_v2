using AutoMapper;
using Sapphire.Auth.Application.DTOs;
using Sapphire.Auth.Domain.Aggregates;
using Sapphire.Auth.Domain.Entities;

namespace Sapphire.Auth.Application.Mappings;

/// <summary>
/// AutoMapper profile for User entity mappings.
/// </summary>
public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.BonusBalance, opt => opt.MapFrom(src => src.BonusBalanceCents / 100m))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.IsBanned, opt => opt.MapFrom(src => src.Status == Domain.Enums.UserStatus.Banned))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles));

        CreateMap<UserRole, RoleDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RoleId))
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystem, opt => opt.Ignore());

        CreateMap<Role, RoleDto>();
    }
}
