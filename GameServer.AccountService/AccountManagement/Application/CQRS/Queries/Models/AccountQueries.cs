using System.ComponentModel.DataAnnotations;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;
using GameServer.AccountService.AccountManagement.Application.DTO;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Models;

public record GetAccountByIdQuery([Required]long Id) : IQuery<AccountDto>
{
    public Guid QueryId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record GetAccountByUsernameQuery([Required] string Username) : IQuery<AccountDto>
{
    public Guid QueryId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record GetAccountByEmailQuery([Required] string Email) : IQuery<AccountDto>
{
    public Guid QueryId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}