using System.Threading.RateLimiting;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

public static class RateLimiterExtension
{
    public static WebApplicationBuilder ConfigureRateLimiter(this WebApplicationBuilder builder)
    {
        // Adicionar Rate Limiting para prevenir abusos na autenticação
        builder.Services.AddRateLimiter(options =>
        {
            // Política global de fallback para outras requisições
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
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
        
        return builder;
    }
    
    public static WebApplication UseApplicationRateLimiter(this WebApplication app)
    {
        // Adicionar o middleware de Rate Limiting
        app.UseRateLimiter();
        
        return app;
    }
}