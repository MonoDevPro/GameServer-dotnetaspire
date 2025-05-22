using GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAccountServices();

var app = builder.Build();

app.UseAccountApplication();

using (var scope = app.Services.CreateScope())
    await OpenIddictExtension.SeedOpenIddictClientsAsync(scope.ServiceProvider);

app.Run();
