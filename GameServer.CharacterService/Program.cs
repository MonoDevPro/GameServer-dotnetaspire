using System.Text;
using System.Threading.RateLimiting;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Authentication;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;
using GameServer.CharacterService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using GameServer.CharacterService.Infrastructure.DependencyInjection;
using GameServer.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Configure authorization
builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Configurar o contexto do banco de dados
builder.Services.AddDbContext<CharacterDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("chardb");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
        });
    });

// Add our hexagonal architecture components
builder.Services.AddHexagonalCharacterService(builder.Configuration);

// Adiciona os serviços de cache
builder.Services.AddCharacterServiceInfrastructure(builder.Configuration);

// Configurar JwtSettings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Configurar UserCacheOptions
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection(CacheSettings.SectionName));

// Configuração para autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
    
    if (jwtSettings == null)
        throw new InvalidOperationException("JWT settings are not configured.");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudiences = jwtSettings.Audiences,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key ?? throw new InvalidOperationException("JWT Key is not configured")))
    };
});

// Adicionar Rate Limiting para prevenir abusos na autenticação
builder.Services.AddRateLimiter(options =>
{
    // Limitar tentativas por minuto por IP
    options.AddFixedWindowLimiter("character-put", rateLimiterOptions =>
    {
        rateLimiterOptions.PermitLimit = 3;
        rateLimiterOptions.Window = TimeSpan.FromMinutes(1);
        rateLimiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        rateLimiterOptions.QueueLimit = 2; // Permitir enfileiramento de até 2 requisições
    });
    options.AddFixedWindowLimiter("character-get", rateLimiterOptions =>
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

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Apply migrations automatically in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CharacterDbContext>();
    dbContext.Database.Migrate();
}

// Aplicar o middleware de rate limiting
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map default health check endpoints
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
