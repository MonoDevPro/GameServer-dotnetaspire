using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;

public class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountDbContext>();
        // Use uma connection string local ou de desenvolvimento
        optionsBuilder.UseNpgsql("Host=localhost;Database=accountdb;Username=postgres;Password=postgres");
        return new AccountDbContext(optionsBuilder.Options);
    }
}