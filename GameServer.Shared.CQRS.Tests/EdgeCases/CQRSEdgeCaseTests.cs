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

namespace GameServer.Shared.CQRS.Tests.EdgeCases;

public class CQRSEdgeCaseTests
{
    [Fact]
    public async Task CommandBus_WithNullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS();
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        // Act & Assert
        var act = async () => await commandBus.SendAsync<EdgeCaseCommand>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task QueryBus_WithNullQuery_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS();
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();

        // Act & Assert
        var act = async () => await queryBus.SendAsync<EdgeCaseQuery, string>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CommandBus_WithUnregisteredHandler_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(); // No handlers registered
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new UnregisteredCommand("Test");

        // Act & Assert
        var act = async () => await commandBus.SendAsync(command);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task QueryBus_WithUnregisteredHandler_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(); // No handlers registered
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();
        var query = new UnregisteredQuery("Test");

        // Act & Assert
        var act = async () => await queryBus.SendAsync<UnregisteredQuery, string>(query);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CommandBus_WithHandlerException_ShouldReturnFailureResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new ExceptionCommand("Test");

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Test exception");
    }

    [Fact]
    public async Task QueryBus_WithHandlerException_ShouldReturnFailureResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();
        var query = new ExceptionQuery("Test");

        // Act
        var result = await queryBus.SendAsync<ExceptionQuery, string>(query);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Test exception");
    }

    [Fact]
    public async Task CommandBus_WithValidationFailure_ShouldReturnValidationError()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        services.AddScoped<IPipelineBehavior<ValidationFailureCommand, Result>, ValidationPipelineBehavior<ValidationFailureCommand>>();
        services.AddScoped<IValidator<ValidationFailureCommand>, ValidationFailureCommandValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new ValidationFailureCommand(""); // Empty name should fail validation

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("validation");
    }

    [Fact]
    public async Task CommandBus_WithCancelledToken_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new DelayCommand("Test");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        // Act & Assert
        var act = async () => await commandBus.SendAsync(command, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryBus_WithCancelledToken_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();
        var query = new DelayQuery("Test");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        // Act & Assert
        var act = async () => await queryBus.SendAsync<DelayQuery, string>(query, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PipelineService_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS();
        var serviceProvider = services.BuildServiceProvider();
        var pipelineService = serviceProvider.GetRequiredService<PipelineService>();

        Task<Result> handler(EdgeCaseCommand request, CancellationToken ct) => Task.FromResult(Result.Success());

        // Act & Assert
        var act = async () => await pipelineService.Execute<EdgeCaseCommand, Result>(null!, handler, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PipelineService_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS();
        var serviceProvider = services.BuildServiceProvider();
        var pipelineService = serviceProvider.GetRequiredService<PipelineService>();
        var command = new EdgeCaseCommand("Test");

        // Act & Assert
        var act = async () => await pipelineService.Execute<EdgeCaseCommand, Result>(command, null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void CQRS_WithCircularDependency_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS();

        // Create a circular dependency scenario
        services.AddScoped<ICircularDependencyA, CircularDependencyA>();
        services.AddScoped<ICircularDependencyB, CircularDependencyB>();

        // Act & Assert
        var act = () => services.BuildServiceProvider();
        act.Should().NotThrow(); // ServiceProvider creation should succeed

        // But getting the service should fail
        var serviceProvider = services.BuildServiceProvider();
        var act2 = () => serviceProvider.GetRequiredService<ICircularDependencyA>();
        act2.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CQRS_WithDisposedServiceProvider_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new EdgeCaseCommand("Test");

        // Act
        serviceProvider.Dispose();

        // Assert
        var act = async () => await commandBus.SendAsync(command);
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CQRS_WithVeryLargePayload_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        // Create a command with a very large payload
        var largeData = new string('A', 1_000_000); // 1MB string
        var command = new LargePayloadCommand(largeData);

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CQRS_WithEmptyString_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new EdgeCaseCommand("");

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CQRS_WithSpecialCharacters_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new EdgeCaseCommand("Special chars: !@#$%^&*()_+-=[]{}|;:,.<>?/~`");

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CQRS_WithUnicodeCharacters_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
        var command = new EdgeCaseCommand("Unicode: 你好世界 🌍🚀💫 العالم мир");

        // Act
        var result = await commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CQRS_WithMaxIntValue_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();
        var query = new NumericQuery(int.MaxValue);

        // Act
        var result = await queryBus.SendAsync<NumericQuery, int>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task CQRS_WithMinIntValue_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCQRS(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var queryBus = serviceProvider.GetRequiredService<IQueryBus>();
        var query = new NumericQuery(int.MinValue);

        // Act
        var result = await queryBus.SendAsync<NumericQuery, int>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(int.MinValue);
    }
}

// Test classes for edge case tests
public record EdgeCaseCommand(string Name) : ICommand;
public record UnregisteredCommand(string Name) : ICommand;
public record ExceptionCommand(string Name) : ICommand;
public record ValidationFailureCommand(string Name) : ICommand;
public record DelayCommand(string Name) : ICommand;
public record LargePayloadCommand(string Data) : ICommand;

public record EdgeCaseQuery(string Name) : IQuery<string>;
public record UnregisteredQuery(string Name) : IQuery<string>;
public record ExceptionQuery(string Name) : IQuery<string>;
public record DelayQuery(string Name) : IQuery<string>;
public record NumericQuery(int Value) : IQuery<int>;

// Circular dependency interfaces
public interface ICircularDependencyA
{
    void DoSomething();
}

public interface ICircularDependencyB
{
    void DoSomethingElse();
}

public class CircularDependencyA : ICircularDependencyA
{
    private readonly ICircularDependencyB _b;

    public CircularDependencyA(ICircularDependencyB b)
    {
        _b = b;
    }

    public void DoSomething()
    {
        _b.DoSomethingElse();
    }
}

public class CircularDependencyB : ICircularDependencyB
{
    private readonly ICircularDependencyA _a;

    public CircularDependencyB(ICircularDependencyA a)
    {
        _a = a;
    }

    public void DoSomethingElse()
    {
        _a.DoSomething();
    }
}

// Edge case handlers
public class EdgeCaseCommandHandler : ICommandHandler<EdgeCaseCommand>
{
    public Task<Result> HandleAsync(EdgeCaseCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}

public class ExceptionCommandHandler : ICommandHandler<ExceptionCommand>
{
    public Task<Result> HandleAsync(ExceptionCommand request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Test exception for {request.Name}");
    }
}

public class ValidationFailureCommandHandler : ICommandHandler<ValidationFailureCommand>
{
    public Task<Result> HandleAsync(ValidationFailureCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}

public class DelayCommandHandler : ICommandHandler<DelayCommand>
{
    public async Task<Result> HandleAsync(DelayCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        return Result.Success();
    }
}

public class LargePayloadCommandHandler : ICommandHandler<LargePayloadCommand>
{
    public Task<Result> HandleAsync(LargePayloadCommand request, CancellationToken cancellationToken)
    {
        // Verify we can handle large payloads
        request.Data.Should().NotBeNull();
        return Task.FromResult(Result.Success());
    }
}

public class EdgeCaseQueryHandler : IQueryHandler<EdgeCaseQuery, string>
{
    public Task<Result<string>> HandleAsync(EdgeCaseQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success($"Result for {request.Name}"));
    }
}

public class ExceptionQueryHandler : IQueryHandler<ExceptionQuery, string>
{
    public Task<Result<string>> HandleAsync(ExceptionQuery request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Test exception for {request.Name}");
    }
}

public class DelayQueryHandler : IQueryHandler<DelayQuery, string>
{
    public async Task<Result<string>> HandleAsync(DelayQuery request, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        return Result<string>.Success($"Delayed result for {request.Name}");
    }
}

public class NumericQueryHandler : IQueryHandler<NumericQuery, int>
{
    public Task<Result<int>> HandleAsync(NumericQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<int>.Success(request.Value));
    }
}

public class ValidationFailureCommandValidator : IValidator<ValidationFailureCommand>
{
    public ValidationResult Validate(ValidationFailureCommand instance)
    {
        if (string.IsNullOrEmpty(instance.Name))
        {
            return new ValidationResult(new ValidationError("Name cannot be empty", nameof(ValidationFailureCommand.Name)));
        }
        return new ValidationResult();
    }

    public Task<ValidationResult> ValidateAsync(ValidationFailureCommand instance, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(instance));
    }

    public ValidationResult Validate(ValidationContext<ValidationFailureCommand> context)
    {
        return Validate(context.Instance);
    }

    public Task<ValidationResult> ValidateAsync(ValidationContext<ValidationFailureCommand> context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(context));
    }
}
