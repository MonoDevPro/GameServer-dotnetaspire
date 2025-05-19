using System.Threading.RateLimiting;
using GameServer.AuthService.Service.Application.Data;
using GameServer.AuthService.Service.Definitions.DependencyInjection;
using GameServer.AuthService.Service.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using GameServer.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

builder.ConfigureAuthServices();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Configure the Swagger with support for both JWT and OpenIddict
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GameServer Auth API",
        Version = "v1",
        Description = "API de autenticação e gerenciamento de usuários do GameServer"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.ConfigureAuthApplication();

// Map default health check endpoints
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
