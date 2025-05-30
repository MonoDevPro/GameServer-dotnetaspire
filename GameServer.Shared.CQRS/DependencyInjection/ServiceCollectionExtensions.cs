using System.Reflection;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Shared.CQRS.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos os serviços CQRS necessários
    /// </summary>
    public static IServiceCollection AddCQRS(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Registrar os serviços de pipeline
        services.AddScoped<IPipelineService, PipelineService>();
        services.AddScoped<CommandPipeline>();
        services.AddScoped<QueryPipeline>();

        // Registrar os buses
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        // Auto-registrar handlers das assemblies fornecidas
        foreach (var assembly in assemblies)
            RegisterHandlers(services, assembly);

        return services;
    }

    /// <summary>
    /// Registra todos os serviços CQRS com behaviors de validação e performance
    /// </summary>
    public static IServiceCollection AddCQRSWithBehaviors(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddCQRS(assemblies);
        services.AddCQRSPipelineBehaviors(assemblies);
        return services;
    }

    /// <summary>
    /// Registra os pipeline behaviors CQRS modernos
    /// </summary>
    public static IServiceCollection AddCQRSPipelineBehaviors(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Registrar validators
        foreach (var assembly in assemblies)
        {
            RegisterValidators(services, assembly);
            RegisterPipelineBehaviors(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Registra os behaviors CQRS (validação, performance, etc.)
    /// </summary>
    public static IServiceCollection AddCQRSBehaviors(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Registrar validators
        foreach (var assembly in assemblies)
            RegisterValidators(services, assembly);

        // Registrar behaviors
        services.AddScoped(typeof(ValidationBehavior<>));
        services.AddScoped(typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(QueryValidationBehavior<,>));
        services.AddScoped(typeof(PerformanceBehavior<>));
        services.AddScoped(typeof(PerformanceBehavior<,>));

        return services;
    }

    /// <summary>
    /// Registra validadores de uma assembly específica
    /// </summary>
    public static IServiceCollection AddCQRSValidators(this IServiceCollection services, Assembly assembly)
    {
        RegisterValidators(services, assembly);
        return services;
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var interfaces = validatorType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .ToList();

            foreach (var interfaceType in interfaces)
                services.AddScoped(interfaceType, validatorType);
        }
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        // Registrar command handlers sem resultado
        var commandHandlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            .ToList();

        foreach (var handlerType in commandHandlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                .ToList();

            foreach (var interfaceType in interfaces)
                services.AddScoped(interfaceType, handlerType);
        }

        // Registrar command handlers com resultado
        var commandHandlerWithResultTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
            .ToList();

        foreach (var handlerType in commandHandlerWithResultTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        // Registrar query handlers
        var queryHandlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
            .ToList();

        foreach (var handlerType in queryHandlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .ToList();

            foreach (var interfaceType in interfaces)
                services.AddScoped(interfaceType, handlerType);
        }
    }

    /// <summary>
    /// Registra apenas os buses CQRS (para cenários onde os handlers são registrados manualmente)
    /// </summary>
    public static IServiceCollection AddCQRSBuses(this IServiceCollection services)
    {
        // Registrar os serviços de pipeline
        services.AddScoped<IPipelineService, PipelineService>();
        services.AddScoped<CommandPipeline>();
        services.AddScoped<QueryPipeline>();

        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();
        return services;
    }

    private static void RegisterPipelineBehaviors(IServiceCollection services, Assembly assembly)
    {
        // Registrar behaviors customizados do assembly
        var behaviorTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
            .ToList();

        foreach (var behaviorType in behaviorTypes)
        {
            var interfaces = behaviorType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                .ToList();

            foreach (var interfaceType in interfaces)
                services.AddScoped(interfaceType, behaviorType);
        }
    }
}
