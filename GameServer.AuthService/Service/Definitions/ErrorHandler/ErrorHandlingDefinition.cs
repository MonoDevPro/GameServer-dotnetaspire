using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Authentication;
using Microsoft.AspNetCore.Diagnostics;

namespace GameServer.AuthService.Service.Definitions.ErrorHandler;

/// <summary>
/// Custom Error handling
/// </summary>
public class ErrorHandlingDefinition
{
    /// <summary>
    /// Configure application for current application
    /// </summary>
    /// <param name="app"></param>
    public static void ConfigureApplication(WebApplication app)
    {
        app.UseExceptionHandler(error => error.Run(async context =>
        {
            context.Response.ContentType = "application/json";
            var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (contextFeature is not null)
            {
                var logger = app.Services.GetService<ILogger<ErrorHandlingDefinition>>();

                // handling all another errors
                logger?.LogError($"Something went wrong in the {contextFeature.Error}");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (app.Environment.IsDevelopment())
                {
                    await context.Response.WriteAsync($"INTERNAL SERVER ERROR: {contextFeature.Error}");
                }
                else
                {
                    await context.Response.WriteAsync("INTERNAL SERVER ERROR. PLEASE TRY AGAIN LATER");
                }
            }
        }));
    }

    private static HttpStatusCode GetErrorCode(Exception e)
        => e switch
        {
            ValidationException _ => HttpStatusCode.BadRequest,
            AuthenticationException _ => HttpStatusCode.Forbidden,
            NotImplementedException _ => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };
}