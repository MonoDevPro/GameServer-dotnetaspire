using GameServer.AccountService.AccountManagement.Application.CQRS.Queries;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Handlers;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Queries.Models;
using GameServer.AccountService.AccountManagement.Application.DTO;
using GameServer.AccountService.AccountManagement.Ports.In;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Container;

public static class CQRSContainerServices
{

    public static WebApplicationBuilder ConfigureCQRS(this WebApplicationBuilder builder)
    {
        // Adiciona os serviços 
        // CQRS QueryBus e Handlers
        builder.Services.AddScoped<IQueryBus, QueryBus>();
        builder.Services.AddScoped<IQueryHandler<GetAccountByIdQuery, AccountDto>, GetAccountByIdQueryHandler>();
        
        return builder;
    }
}
