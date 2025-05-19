using System.Security.Claims;
using GameServer.AuthService.Service.Application.Models;
using GameServer.AuthService.Service.Domain.Entities;
using GameServer.AuthService.Service.Infrastructure.Helper;

namespace GameServer.AuthService.Service.Infrastructure.Adapters.Out.Mapper;

public static class ProfilesMapping
{
    public static ApplicationUser ToProfile(this ClaimsIdentity claims)
    {
        var userProfileViewModel = new ApplicationUser
        {
            Id = ClaimsHelper.GetValue<Guid>(claims, ClaimTypes.Name),
            PositionName = ClaimsHelper.GetValue<string>(claims, ClaimTypes.Actor),
            FirstName = ClaimsHelper.GetValue<string>(claims, ClaimTypes.GivenName),
            LastName = ClaimsHelper.GetValue<string>(claims, ClaimTypes.Surname),
            Roles = ClaimsHelper.GetValues<string>(claims, ClaimTypes.Role),
            Email = ClaimsHelper.GetValue<string>(claims, ClaimTypes.Name),
            PhoneNumber = ClaimsHelper.GetValue<string>(claims, ClaimTypes.MobilePhone)
        };
        
        return userProfileViewModel;
    }

    public static UserDto ToDto(this ApplicationUser profile)
    {
        return new UserDto(profile);
    }
}
