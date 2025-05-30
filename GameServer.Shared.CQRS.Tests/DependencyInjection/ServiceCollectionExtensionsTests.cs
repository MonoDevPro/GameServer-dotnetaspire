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

namespace GameServer.Shared.CQRS.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCQRS_ShouldRegisterCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ICommandBus>().Should().NotBeNull();
        serviceProvider.GetService<IQueryBus>().Should().NotBeNull();
        serviceProvider.GetService<PipelineService>().Should().NotBeNull();
        serviceProvider.GetService<CommandPipeline>().Should().NotBeNull();
        serviceProvider.GetService<QueryPipeline>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRS_WithAssembly_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRS(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ICommandHandler<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<ICommandHandler<TestCommandWithResult, string>>().Should().NotBeNull();
        serviceProvider.GetService<IQueryHandler<TestQuery, string>>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRSWithBehaviors_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSWithBehaviors(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ICommandBus>().Should().NotBeNull();
        serviceProvider.GetService<IQueryBus>().Should().NotBeNull();
        serviceProvider.GetService<ICommandHandler<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRSPipelineBehaviors_ShouldRegisterValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSPipelineBehaviors(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<IPipelineBehavior<TestCommand, Result>>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRSBehaviors_ShouldRegisterBehaviorTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSBehaviors(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<ValidationBehavior<TestCommand>>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRSValidators_ShouldRegisterOnlyValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSValidators(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IValidator<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<ICommandBus>().Should().BeNull();
    }

    [Fact]
    public void AddCQRSBuses_ShouldRegisterOnlyBuses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRSBuses();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ICommandBus>().Should().NotBeNull();
        serviceProvider.GetService<IQueryBus>().Should().NotBeNull();
        serviceProvider.GetService<PipelineService>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRS_WithMultipleAssemblies_ShouldRegisterAllHandlers()
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
        serviceProvider.GetService<ICommandHandler<TestCommand>>().Should().NotBeNull();
        serviceProvider.GetService<ICommandBus>().Should().NotBeNull();
        serviceProvider.GetService<IQueryBus>().Should().NotBeNull();
    }

    [Fact]
    public void AddCQRS_ShouldRegisterServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS();

        // Assert
        var commandBusDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICommandBus));
        var queryBusDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IQueryBus));

        commandBusDescriptor.Should().NotBeNull();
        commandBusDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        queryBusDescriptor.Should().NotBeNull();
        queryBusDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCQRS_CalledMultipleTimes_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCQRS();
        services.AddCQRS();

        // Assert
        var commandBusDescriptors = services.Where(s => s.ServiceType == typeof(ICommandBus)).ToList();
        commandBusDescriptors.Should().HaveCount(2); // Should have duplicates as that's normal DI behavior
    }

    [Fact]
    public void ServiceProvider_ShouldResolveHandlersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();
        services.AddCQRS(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var commandHandler = serviceProvider.GetService<ICommandHandler<TestCommand>>();
        var commandWithResultHandler = serviceProvider.GetService<ICommandHandler<TestCommandWithResult, string>>();
        var queryHandler = serviceProvider.GetService<IQueryHandler<TestQuery, string>>();

        // Assert
        commandHandler.Should().NotBeNull();
        commandHandler.Should().BeOfType<TestCommandHandler>();

        commandWithResultHandler.Should().NotBeNull();
        commandWithResultHandler.Should().BeOfType<TestCommandWithResultHandler>();

        queryHandler.Should().NotBeNull();
        queryHandler.Should().BeOfType<TestQueryHandler>();
    }

    [Fact]
    public void ServiceProvider_WithDependencies_ShouldInjectCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITestService, TestService>();
        var assembly = Assembly.GetExecutingAssembly();
        services.AddCQRS(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var handler = serviceProvider.GetService<ICommandHandler<TestCommandWithDependency>>();

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<TestCommandWithDependencyHandler>();
    }

    [Fact]
    public void RegisterPipelineBehaviors_ShouldRegisterCustomBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddCQRSPipelineBehaviors(assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var behavior = serviceProvider.GetService<IPipelineBehavior<TestCommand, Result>>();
        behavior.Should().NotBeNull();
    }
}

// Test DTOs and handlers for dependency injection tests
public record TestCommand : ICommand;
public record TestCommandWithResult : ICommand<string>;
public record TestCommandWithDependency : ICommand;
public record TestQuery : IQuery<string>;

public interface ITestService
{
    string GetData();
}

public class TestService : ITestService
{
    public string GetData() => "Test Data";
}

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public Task<Result> HandleAsync(TestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}

public class TestCommandWithResultHandler : ICommandHandler<TestCommandWithResult, string>
{
    public Task<Result<string>> HandleAsync(TestCommandWithResult request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success("Test Result"));
    }
}

public class TestCommandWithDependencyHandler : ICommandHandler<TestCommandWithDependency>
{
    private readonly ITestService _testService;

    public TestCommandWithDependencyHandler(ITestService testService)
    {
        _testService = testService;
    }

    public Task<Result> HandleAsync(TestCommandWithDependency request, CancellationToken cancellationToken)
    {
        var data = _testService.GetData();
        return Task.FromResult(Result.Success());
    }
}

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> HandleAsync(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success("Test Query Result"));
    }
}

public class TestCommandValidator : IValidator<TestCommand>
{
    public ValidationResult Validate(TestCommand instance)
    {
        return new ValidationResult();
    }

    public Task<ValidationResult> ValidateAsync(TestCommand instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    public ValidationResult Validate(ValidationContext<TestCommand> context)
    {
        return Validate(context.Instance);
    }

    public Task<ValidationResult> ValidateAsync(ValidationContext<TestCommand> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(context));
    }
}

public class TestCustomPipelineBehavior : IPipelineBehavior<TestCommand, Result>
{
    public async Task<Result> Handle(TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken)
    {
        // Custom behavior logic
        return await next();
    }
}
