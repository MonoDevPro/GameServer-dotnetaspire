using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GameServer.AuthService.Infrastructure.Adapters.Out.Authentication;
using GameServer.AuthService.Infrastructure.Adapters.Out.Cache;
using GameServer.AuthService.Infrastructure.DependencyInjection;

using GameServer.AuthService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using GameServer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// Configurar o contexto do banco de dados
builder.Services.AddDbContext<AuthDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("authdb");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
        });
    });

// Adicionar Rate Limiting para prevenir abusos na autenticação
builder.Services.AddRateLimiter(options =>
{
    // Limitar tentativas de login para 5 por minuto por IP
    options.AddFixedWindowLimiter("login", rateLimiterOptions =>
    {
        rateLimiterOptions.PermitLimit = 5;
        rateLimiterOptions.Window = TimeSpan.FromMinutes(1);
        rateLimiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        rateLimiterOptions.QueueLimit = 2; // Permitir enfileiramento de até 2 requisições
    });
    
    // Política global de fallback para outras requisições
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(10)
            });
    });
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Taxa de requisições excedida. Por favor, tente novamente mais tarde."
        }, token);
    };
});

// Configurar JwtSettings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Configurar UserCacheOptions
builder.Services.Configure<UserCacheOptions>(
    builder.Configuration.GetSection(UserCacheOptions.SectionName));

// Configuração para autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
    };
});

// Configurar o Memory Cache do pacote Microsoft.Extensions.Caching.Memory
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = builder.Configuration.GetSection("UserCache:SizeLimit").Get<long?>() ?? 1000;
});

// Adicionar os serviços do microserviço
builder.Services.AddHexagonalAuthService(builder.Configuration);

// Configurar o Swagger com suporte a autenticação JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GameServer Auth API",
        Version = "v1",
        Description = "API de autenticação e gerenciamento de usuários do GameServer"
    });

    // Adiciona uma definição de segurança para o JWT
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Aplica a segurança globalmente a todos os endpoints
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Aplicar o middleware de rate limiting
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map default health check endpoints
app.MapDefaultEndpoints();
app.MapControllers();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
    
    // Apply migrations automatically in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
