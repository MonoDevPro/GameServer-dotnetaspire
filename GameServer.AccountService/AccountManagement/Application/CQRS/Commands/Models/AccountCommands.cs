using System.ComponentModel.DataAnnotations;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Domain.Enums;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Models;

    
    public record RegisterCommand(
        [Required] string Username,
        [Required] string Email,
        [Required] string Password
    ) : ICommand<ResultCommand>;