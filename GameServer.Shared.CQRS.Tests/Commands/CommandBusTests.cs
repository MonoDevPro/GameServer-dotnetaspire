using FluentAssertions;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Commands;

public class CommandBusTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ICommandBus _commandBus;

    public CommandBusTests()
    {
        var services = new ServiceCollection();

        // Register core services
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<ICommandBus, CommandBus>();

        // Register pipeline services
        services.AddScoped<IPipelineService, PipelineService>();
        services.AddScoped<CommandPipeline>();

        // Registrar handlers de teste
        services.AddTransient<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddTransient<ICommandHandler<TestCommandWithResult, string>, TestCommandWithResultHandler>();
        services.AddTransient<ICommandHandler<ExceptionThrowingCommand>, ExceptionThrowingCommandHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
    }

    [Fact]
    public async Task SendAsync_WithValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var command = new TestCommand("test");

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithValidCommandWithResult_ShouldReturnExpectedResult()
    {
        // Arrange
        var command = new TestCommandWithResult("test");
        var expectedValue = "processed: test";

        // Act
        var result = await _commandBus.SendAsync<TestCommandWithResult, string>(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
    }

    [Fact]
    public async Task SendAsync_WithUnregisteredCommand_ShouldReturnFailure()
    {
        // Arrange
        var command = new UnregisteredCommand();

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Contains("Nenhum handler encontrado"));
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var result = await _commandBus.SendAsync(command, cancellationTokenSource.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Operação cancelada");
    }

    [Fact]
    public async Task SendAsync_WithExceptionInHandler_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new ExceptionThrowingCommand();

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Contains("Erro interno"));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<CommandBus>>();
        var pipeline = _serviceProvider.GetRequiredService<CommandPipeline>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommandBus(null!, logger, pipeline));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var pipeline = _serviceProvider.GetRequiredService<CommandPipeline>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommandBus(_serviceProvider, null!, pipeline));
    }

    [Fact]
    public void Constructor_WithNullPipeline_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = _serviceProvider.GetRequiredService<ILogger<CommandBus>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CommandBus(_serviceProvider, logger, null!));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    // Test Command Classes
    public record TestCommand(string Value) : ICommand;
    public record TestCommandWithResult(string Value) : ICommand<string>;
    public record UnregisteredCommand : ICommand;
    public record ExceptionThrowingCommand : ICommand;

    // Test Handlers
    public class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task<Result> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Result.Success());
        }
    }

    public class TestCommandWithResultHandler : ICommandHandler<TestCommandWithResult, string>
    {
        public Task<Result<string>> HandleAsync(TestCommandWithResult command, CancellationToken cancellationToken = default)
        {
            var result = $"processed: {command.Value}";
            return Task.FromResult(Result<string>.Success(result));
        }
    }

    public class ExceptionThrowingCommandHandler : ICommandHandler<ExceptionThrowingCommand>
    {
        public Task<Result> HandleAsync(ExceptionThrowingCommand command, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}
