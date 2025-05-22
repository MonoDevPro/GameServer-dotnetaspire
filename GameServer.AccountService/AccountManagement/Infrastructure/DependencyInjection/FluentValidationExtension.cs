using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.AccountService.AccountManagement.Infrastructure.DependencyInjection;

/// <summary>
/// FluentValidation registration as MicroserviceDefinition
/// </summary>
public static class FluentValidationExtension
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public static WebApplicationBuilder ConfigureFluentValidation(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        
        return builder;
    }
}