using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.AuthService.Service.Definitions.FluentValidating;

/// <summary>
/// FluentValidation registration as MicroserviceDefinition
/// </summary>
public static class FluentValidationDefinition
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
    }
}