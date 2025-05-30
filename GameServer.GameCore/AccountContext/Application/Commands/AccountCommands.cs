using System.ComponentModel.DataAnnotations;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Results;

namespace GameServer.GameCore.AccountContext.Application.Commands;

public class AccountCommands
{
    public record RegisterCommand(
        [Required] string Username,
        [Required] string Email,
        [Required] string Password,
        string? IpAddress
    ) : ICommand<Result>;
    
    public record LoginCommand(
        [Required] string UsernameOrEmail,
        [Required] string Password,
        string? IpAddress
    ) : ICommand<Result>;

    public record ChangePasswordCommand(
        [Required] long AccountId,
        [Required] string CurrentPassword,
        [Required] string NewPassword,
        string? IpAddress
    ) : ICommand<Result>;
}