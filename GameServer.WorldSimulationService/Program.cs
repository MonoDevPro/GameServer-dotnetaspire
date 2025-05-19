using System.Text;
using System.Threading.RateLimiting;
using GameServer.ServiceDefaults;
using GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Authentication;
using GameServer.WorldSimulationService.Infrastructure.Adapters.Out.Persistence.EntityFramework;
using GameServer.WorldSimulationService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Configure authorization
builder.Services.AddAuthorization();

// Add controllers and gRPC services
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Configure database context
builder.Services.AddDbContext<WorldDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("worlddb");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
    });
});

// Add our hexagonal architecture components
builder.Services.AddHexagonalWorldSimulationService(builder.Configuration);

// Configure JwtSettings for authentication
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Configure authentication with JWT
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
            Encoding.UTF8.GetBytes(jwtSettings.Key?? throw new InvalidOperationException("JWT Key is not configured")))
    };
});

// Add rate limiting to prevent abuse
builder.Services.AddRateLimiter(options =>
{
    // Limit commands per minute per player
    options.AddFixedWindowLimiter("movement-commands", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30; // 30 movement commands per minute (higher than other services due to real-time nature)
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Global fallback policy for other requests
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromSeconds(10)
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded. Please try again later."
        }, token);
    };
});

// Configure Swagger with JWT authentication support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GameServer World Simulation API",
        Version = "v1",
        Description = "API for world simulation in the GameServer"
    });

    // Add security definition for JWT
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Apply security globally to all endpoints
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

// Configure the HTTP request pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Enable gRPC reflection in development for tools like grpcurl
    app.MapGrpcReflectionService();

    // Apply migrations automatically in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorldDbContext>();
    dbContext.Database.Migrate();
}

// Apply rate limiting middleware
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and gRPC services
app.MapControllers();
app.MapGrpcService<GameServer.WorldSimulationService.Infrastructure.Adapters.In.Grpc.WorldSimulationGrpcService>();

// Map default health check endpoints
app.MapDefaultEndpoints();

app.Run();
