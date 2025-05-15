using Microsoft.EntityFrameworkCore;
using GameServer.AuthService.Domain.Entities;

namespace GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.EntityFramework;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}