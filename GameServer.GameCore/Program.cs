using GameServer.GameCore;
using GameServer.GameCore.AccountContext.Infrastructure.Bootstraping;
using GameServer.Shared.Database.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add services to the container.
builder.Services.AddLogging(configure => configure.AddConsole());

builder.Services.AddAccountServices();



builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();