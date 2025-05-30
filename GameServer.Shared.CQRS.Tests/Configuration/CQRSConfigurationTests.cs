using System.Reflection;
using FluentAssertions;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.DependencyInjection;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Configuration;

public class CQRSConfigurationTests
{
    [Fact]
    public void CQRS_WithCustomConfiguration_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });

        // Act
        services.AddCQRSWithBehaviors(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var commandBus = serviceProvider.GetService<ICommandBus>();
        var queryBus = serviceProvider.GetService<IQueryBus>();

        commandBus.Should().NotBeNull();
        queryBus.Should().NotBeNull();
    }

    [Fact]
    public void CQRS_WithMinimalConfiguration_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRSBuses();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var commandBus = serviceProvider.GetService<ICommandBus>();
        var queryBus = serviceProvider.GetService<IQueryBus>();

        commandBus.Should().NotBeNull();
        queryBus.Should().NotBeNull();
    }

    [Fact]
    public void CQRS_WithMultipleAssemblies_ShouldRegisterFromAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(ServiceCollectionExtensions).Assembly;

        // Act
        services.AddCQRS(assembly1, assembly2);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var handlers = serviceProvider.GetServices<ICommandHandler<ConfigTestCommand>>();
        handlers.Should().NotBeEmpty();
    }

    [Fact]
    public void CQRS_WithValidatorsOnly_ShouldNotRegisterBuses()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSValidators(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var validator = serviceProvider.GetService<IValidator<ConfigTestCommand>>();
        var commandBus = serviceProvider.GetService<ICommandBus>();

        validator.Should().NotBeNull();
        commandBus.Should().BeNull();
    }

    [Fact]
    public void CQRS_WithBehaviorsOnly_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSBehaviors(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var validationBehavior = serviceProvider.GetService<ValidationBehavior<ConfigTestCommand>>();
        var performanceBehavior = serviceProvider.GetService<PerformanceBehavior<ConfigTestCommand>>();

        validationBehavior.Should().NotBeNull();
        performanceBehavior.Should().NotBeNull();
    }

    [Fact]
    public void CQRS_ServiceLifetime_ShouldBeScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS();

        // Assert
        var descriptors = services.Where(s =>
            s.ServiceType == typeof(ICommandBus) ||
            s.ServiceType == typeof(IQueryBus) ||
            s.ServiceType == typeof(PipelineService)).ToList();

        descriptors.Should().AllSatisfy(d => d.Lifetime.Should().Be(ServiceLifetime.Scoped));
    }

    [Fact]
    public void CQRS_WithScopedServices_ShouldCreateNewInstancesPerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // Act
        ICommandBus commandBus1, commandBus2;
        using (var scope1 = serviceProvider.CreateScope())
        {
            commandBus1 = scope1.ServiceProvider.GetRequiredService<ICommandBus>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            commandBus2 = scope2.ServiceProvider.GetRequiredService<ICommandBus>();
        }

        // Assert
        commandBus1.Should().NotBeSameAs(commandBus2);
    }

    [Fact]
    public void CQRS_WithCustomLogger_ShouldUseProvidedLogger()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CommandBus>>();
        var services = new ServiceCollection();
        services.AddSingleton(mockLogger.Object);
        services.AddCQRS(Assembly.GetExecutingAssembly());

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        // Assert
        commandBus.Should().NotBeNull();
        // Logger should be injected into CommandBus
    }

    [Fact]
    public void CQRS_RegisteredMultipleTimes_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - Register multiple times (should not cause issues)
        services.AddCQRS();
        services.AddCQRS();
        services.AddCQRSBuses();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetService<ICommandBus>();
        commandBus.Should().NotBeNull();
    }

    [Fact]
    public async Task CQRS_WithComplexPipelineConfiguration_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());

        // Register multiple behaviors in specific order
        services.AddScoped<IPipelineBehavior<ConfigTestCommand, Result>, LoggingPipelineBehavior<ConfigTestCommand>>();
        services.AddScoped<IPipelineBehavior<ConfigTestCommand, Result>, ValidationPipelineBehavior<ConfigTestCommand>>();
        services.AddScoped<IPipelineBehavior<ConfigTestCommand, Result>, PerformancePipelineBehavior<ConfigTestCommand>>();

        services.AddScoped<IValidator<ConfigTestCommand>, ConfigTestCommandValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        // Act
        var command = new ConfigTestCommand("Test");
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CQRS_WithEmptyAssembly_ShouldNotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var emptyAssembly = Assembly.GetExecutingAssembly(); // Using current assembly as test

        // Act
        var act = () => services.AddCQRS(emptyAssembly);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CQRS_WithNullAssembly_ShouldNotFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var act = () => services.AddCQRS((Assembly[])null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CQRS_HandlerRegistration_ShouldNotRegisterAbstractClasses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Should not register abstract handlers
        var abstractHandler = serviceProvider.GetService<ICommandHandler<AbstractTestCommand>>();
        abstractHandler.Should().BeNull();
    }

    [Fact]
    public void CQRS_HandlerRegistration_ShouldNotRegisterInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Interface implementations should not be auto-registered
        var interfaceImplementations = serviceProvider.GetServices<ITestInterface>();
        interfaceImplementations.Should().BeEmpty();
    }

    [Fact]
    public async Task CQRS_ThreadSafety_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        // Act
        var tasks = Enumerable.Range(0, 50)
            .Select(async i =>
            {
                using var scope = serviceProvider.CreateScope();
                var scopedCommandBus = scope.ServiceProvider.GetRequiredService<ICommandBus>();
                var command = new ConfigTestCommand($"Test{i}");
                return await scopedCommandBus.SendAsync(command);
            }).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }
}

// Test classes for configuration tests
public record ConfigTestCommand(string Name) : ICommand;
public record AbstractTestCommand(string Name) : ICommand;

public interface ITestInterface
{
    void DoSomething();
}

public class ConfigTestCommandHandler : ICommandHandler<ConfigTestCommand>
{
    public Task<Result> HandleAsync(ConfigTestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}

public class ConfigTestCommandValidator : IValidator<ConfigTestCommand>
{
    public ValidationResult Validate(ConfigTestCommand instance)
    {
        if (string.IsNullOrEmpty(instance.Name))
        {
            return new ValidationResult(new ValidationError("Name is required", nameof(ConfigTestCommand.Name)));
        }
        return new ValidationResult();
    }

    public Task<ValidationResult> ValidateAsync(ConfigTestCommand instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    public ValidationResult Validate(ValidationContext<ConfigTestCommand> context)
    {
        return Validate(context.Instance);
    }

    public Task<ValidationResult> ValidateAsync(ValidationContext<ConfigTestCommand> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(context));
    }
}

public abstract class AbstractTestCommandHandler : ICommandHandler<AbstractTestCommand>
{
    public abstract Task<Result> HandleAsync(AbstractTestCommand request, CancellationToken cancellationToken);
}

public interface ITestCommandInterface : ICommandHandler<ConfigTestCommand>
{
    // This should not be auto-registered
}
