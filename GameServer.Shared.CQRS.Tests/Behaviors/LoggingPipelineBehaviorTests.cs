using FluentAssertions;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Behaviors;

public class LoggingPipelineBehaviorTests
{
    private readonly Mock<ILogger<LoggingPipelineBehavior<TestCommand>>> _loggerMock;
    private readonly LoggingPipelineBehavior<TestCommand> _behavior;

    public LoggingPipelineBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingPipelineBehavior<TestCommand>>>();
        _behavior = new LoggingPipelineBehavior<TestCommand>(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new LoggingPipelineBehavior<TestCommand>(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_WithSuccessfulExecution_ShouldLogStartAndSuccess()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify start logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🚀 Iniciando execução do comando TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify success logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("✅ Comando TestCommand executado com sucesso")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailureResult_ShouldLogStartAndFailure()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Failure("Test error");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify start logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🚀 Iniciando execução do comando TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify failure logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("⚠️ Comando TestCommand executado com falha")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithException_ShouldLogStartAndError()
    {
        // Arrange
        var command = new TestCommand("test");
        var exception = new InvalidOperationException("Test exception");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => throw exception;

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(command, next, cancellationToken));

        thrownException.Should().BeSameAs(exception);

        // Verify start logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🚀 Iniciando execução do comando TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("❌ Erro inesperado durante execução do comando TestCommand")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseLoggingScope()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        await _behavior.Handle(command, next, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.BeginScope(It.Is<string>(s => s == "Processing command {CommandName}"), "TestCommand"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleErrors_ShouldLogAllErrors()
    {
        // Arrange
        var command = new TestCommand("test");
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var expectedResult = Result.Failure(errors);
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify failure logging with all errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error 1, Error 2, Error 3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellation_ShouldStillLogCorrectly()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cts.Token);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify start logging still occurs
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("🚀 Iniciando execução do comando TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Test command for testing
    public record TestCommand(string Value) : ICommand;
}
