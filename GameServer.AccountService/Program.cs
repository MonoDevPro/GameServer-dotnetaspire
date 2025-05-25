using GameServer.AccountService.AccountManagement.Adapters.Out.Messaging;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence.UnityOfWork;
using GameServer.AccountService.AccountManagement.Application.CQRS.Container;
using GameServer.AccountService.AccountManagement.Ports.Out.Messaging;
using GameServer.AccountService.AccountManagement.Ports.Out.Persistence;
using GameServer.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

builder.Services.AddUnitOfWork<AccountDbContext>();

builder.Services.AddScoped<IAccountRepository, AccountEfRepository>();

// Connection string for database overriding by Aspire
var connectionString = builder.Configuration.GetConnectionString("accountdb");
// Use the connection string named "accountdb" (will be injected by Aspire)

// Configure the database context
builder.Services.AddDbContext<AccountDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString))
        options.UseInMemoryDatabase("accountdb-local");
    else
        options.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure(3));
});

// Adiciona os serviços de CQRS
builder.ConfigureCQRS();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Migrate the database
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    dbContext.Database.Migrate();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.Run();
