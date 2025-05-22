using System.ComponentModel.DataAnnotations;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Models;

public record AuthenticateCommand(
    [Required] string UsernameOrEmail,
    [Required] string Password
) : ICommand<ResultCommand>;

public record LogoutCommand(
    [Required] string Username
) : ICommand<ResultCommand>;