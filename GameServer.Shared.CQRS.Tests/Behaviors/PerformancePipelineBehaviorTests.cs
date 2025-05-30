using FluentAssertions;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Behaviors;

public class PerformancePipelineBehaviorTests
{
    private readonly Mock<ILogger<PerformancePipelineBehavior<TestCommand>>> _loggerMock;
    private readonly PerformancePipelineBehavior<TestCommand> _behavior;

    public PerformancePipelineBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<PerformancePipelineBehavior<TestCommand>>>();
        _behavior = new PerformancePipelineBehavior<TestCommand>(_loggerMock.Object, 1000);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PerformancePipelineBehavior<TestCommand>(null!, 1000);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var behavior = new PerformancePipelineBehavior<TestCommand>(_loggerMock.Object, 500);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDefaultThreshold_ShouldUseDefaultValue()
    {
        // Act
        var behavior = new PerformancePipelineBehavior<TestCommand>(_loggerMock.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithFastExecution_ShouldLogDebug()
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

        // Verify debug logging for start
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando monitoramento de performance para comando TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify debug logging for completion (fast execution)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommand executado com sucesso")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSlowExecution_ShouldLogWarning()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;
        var behavior = new PerformancePipelineBehavior<TestCommand>(_loggerMock.Object, 10); // Very low threshold

        RequestHandlerDelegate<Result> next = async () =>
        {
            await Task.Delay(50, cancellationToken); // Delay to exceed threshold
            return expectedResult;
        };

        // Act
        var result = await behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify warning logging for slow execution
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("⚠️ Comando TestCommand executado com sucesso") &&
                                              v.ToString()!.Contains("LENTO - acima do limite")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMediumExecution_ShouldLogInformation()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;
        var behavior = new PerformancePipelineBehavior<TestCommand>(_loggerMock.Object, 100); // Medium threshold

        RequestHandlerDelegate<Result> next = async () =>
        {
            await Task.Delay(60, cancellationToken); // Delay between half and full threshold
            return expectedResult;
        };

        // Act
        var result = await behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify information logging for medium execution
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommand executado com sucesso")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailureResult_ShouldLogFailureStatus()
    {
        // Arrange
        var command = new TestCommand("test");
        var failureResult = Result.Failure("Test error");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result> next = () => Task.FromResult(failureResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(failureResult);

        // Verify logging shows failure status
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommand executado com falha")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithException_ShouldLogErrorAndRethrow()
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

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommand falhou") &&
                                              v.ToString()!.Contains("com exceção")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellation_ShouldHandleCancellation()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        RequestHandlerDelegate<Result> next = async () =>
        {
            await Task.Delay(1000, cancellationTokenSource.Token);
            return Result.Success();
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _behavior.Handle(command, next, cancellationTokenSource.Token));
    }

    // Test command for testing
    public record TestCommand(string Value) : ICommand;
}

public class PerformancePipelineBehaviorWithResultTests
{
    private readonly Mock<ILogger<PerformancePipelineBehavior<TestCommandWithResult, string>>> _loggerMock;
    private readonly PerformancePipelineBehavior<TestCommandWithResult, string> _behavior;

    public PerformancePipelineBehaviorWithResultTests()
    {
        _loggerMock = new Mock<ILogger<PerformancePipelineBehavior<TestCommandWithResult, string>>>();
        _behavior = new PerformancePipelineBehavior<TestCommandWithResult, string>(_loggerMock.Object, 1000);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PerformancePipelineBehavior<TestCommandWithResult, string>(null!, 1000);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_WithSuccessResult_ShouldLogSuccessStatus()
    {
        // Arrange
        var command = new TestCommandWithResult("test");
        var expectedResult = Result<string>.Success("test result");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify debug logging for completion
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommandWithResult executado com sucesso")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailureResult_ShouldLogFailureStatus()
    {
        // Arrange
        var command = new TestCommandWithResult("test");
        var failureResult = Result<string>.Failure("Test error");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(failureResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(failureResult);

        // Verify logging shows failure status
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommandWithResult executado com falha")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Test command with result for testing
    public record TestCommandWithResult(string Value) : ICommand<string>;
}

public class QueryPerformancePipelineBehaviorTests
{
    private readonly Mock<ILogger<QueryPerformancePipelineBehavior<TestQuery, string>>> _loggerMock;
    private readonly QueryPerformancePipelineBehavior<TestQuery, string> _behavior;

    public QueryPerformancePipelineBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<QueryPerformancePipelineBehavior<TestQuery, string>>>();
        _behavior = new QueryPerformancePipelineBehavior<TestQuery, string>(_loggerMock.Object, 500);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryPerformancePipelineBehavior<TestQuery, string>(null!, 500);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithDefaultThreshold_ShouldUseDefaultValue()
    {
        // Act
        var behavior = new QueryPerformancePipelineBehavior<TestQuery, string>(_loggerMock.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithFastQuery_ShouldLogDebug()
    {
        // Arrange
        var query = new TestQuery("test");
        var expectedResult = Result<string>.Success("test result");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(query, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify debug logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando monitoramento de performance para query TestQuery")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSlowQuery_ShouldLogWarning()
    {
        // Arrange
        var query = new TestQuery("test");
        var expectedResult = Result<string>.Success("test result");
        var cancellationToken = CancellationToken.None;
        var behavior = new QueryPerformancePipelineBehavior<TestQuery, string>(_loggerMock.Object, 10); // Very low threshold

        RequestHandlerDelegate<Result<string>> next = async () =>
        {
            await Task.Delay(50, cancellationToken); // Delay to exceed threshold
            return expectedResult;
        };

        // Act
        var result = await behavior.Handle(query, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify warning logging for slow execution
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("⚠️ Query TestQuery executada com sucesso") &&
                                              v.ToString()!.Contains("LENTA - acima do limite")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithQueryException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var query = new TestQuery("test");
        var exception = new InvalidOperationException("Query exception");
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<Result<string>> next = () => throw exception;

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(query, next, cancellationToken));

        thrownException.Should().BeSameAs(exception);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Query TestQuery falhou") &&
                                              v.ToString()!.Contains("com exceção")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Test query for testing
    public record TestQuery(string Value) : IQuery<string>;
}
