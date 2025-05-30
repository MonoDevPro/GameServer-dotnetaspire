using GameServer.GameCore.AccountContext.Adapters.Out.Persistence;
using GameServer.GameCore.AccountContext.Domain.Entities;
using GameServer.GameCore.AccountContext.Ports.Out.Persistence;
using GameServer.Shared.Database.DependencyInjection;
using GameServer.Shared.Database.Repository.Writer;
using Microsoft.EntityFrameworkCore;

namespace GameServer.GameCore.AccountContext.Infrastructure.Bootstraping;

public static class AccountServicesExtension
{
    public static IServiceCollection AddAccountServices(this IServiceCollection services)
    {
        // Adiciona o DbContext do Account
        services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=accountdb;Username=postgres;Password=postgres"));

        services.ConfigureDatabaseServices<Account, AccountDbContext>();
        
        // Registra o repositório de escrita
        services.AddScoped<IAccountRepositoryWriter, AccountRepositoryWriter>();

        // Registra o repositório de leitura
        services.AddScoped<IAccountRepositoryReader, AccountRepositoryReader>();

        return services;
    }
}