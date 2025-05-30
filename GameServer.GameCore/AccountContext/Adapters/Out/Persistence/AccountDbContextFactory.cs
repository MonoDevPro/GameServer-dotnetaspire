using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameServer.GameCore.AccountContext.Adapters.Out.Persistence;

public class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountDbContext>();

        // Usa a connection string passada por argumento, ou um valor padrão
        var connectionString = args.Length > 0
            ? args[0]
            : "Host=localhost;Database=accountdb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new AccountDbContext(optionsBuilder.Options);
    }
}