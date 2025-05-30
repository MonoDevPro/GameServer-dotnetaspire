using FluentAssertions;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Pipeline;

public class PipelineServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<PipelineService>> _loggerMock;
    private readonly PipelineService _pipelineService;

    public PipelineServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<PipelineService>>();
        _pipelineService = new PipelineService(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PipelineService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new PipelineService(_serviceProviderMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Execute_WithNoBehaviors_ShouldExecuteHandlerDirectly()
    {
        // Arrange
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("response");
        var cancellationToken = CancellationToken.None;

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Returns(Array.Empty<IPipelineBehavior<TestRequest, TestResponse>>());

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _pipelineService.Execute(request, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResponse);

        // Verify debug logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nenhum behavior encontrado para TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithSingleBehavior_ShouldExecuteBehaviorAndHandler()
    {
        // Arrange
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("response");
        var cancellationToken = CancellationToken.None;

        var behaviorMock = new Mock<IPipelineBehavior<TestRequest, TestResponse>>();
        behaviorMock
            .Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken))
            .Returns<TestRequest, RequestHandlerDelegate<TestResponse>, CancellationToken>(
                async (req, next, ct) => await next());

        var behaviors = new[] { behaviorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Returns(behaviors);

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _pipelineService.Execute(request, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResponse);

        // Verify behavior was called
        behaviorMock.Verify(
            b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken),
            Times.Once);

        // Verify debug logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executando 1 behaviors para TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithMultipleBehaviors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("response");
        var cancellationToken = CancellationToken.None;
        var executionOrder = new List<string>();

        var behavior1Mock = new Mock<IPipelineBehavior<TestRequest, TestResponse>>();
        behavior1Mock
            .Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken))
            .Returns<TestRequest, RequestHandlerDelegate<TestResponse>, CancellationToken>(
                async (req, next, ct) =>
                {
                    executionOrder.Add("Behavior1-Before");
                    var result = await next();
                    executionOrder.Add("Behavior1-After");
                    return result;
                });

        var behavior2Mock = new Mock<IPipelineBehavior<TestRequest, TestResponse>>();
        behavior2Mock
            .Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken))
            .Returns<TestRequest, RequestHandlerDelegate<TestResponse>, CancellationToken>(
                async (req, next, ct) =>
                {
                    executionOrder.Add("Behavior2-Before");
                    var result = await next();
                    executionOrder.Add("Behavior2-After");
                    return result;
                });

        var behaviors = new[] { behavior1Mock.Object, behavior2Mock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Returns(behaviors);

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) =>
        {
            executionOrder.Add("Handler");
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await _pipelineService.Execute(request, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResponse);

        // Verify execution order (pipeline is built in reverse, so Behavior2 executes first)
        executionOrder.Should().ContainInOrder(
            "Behavior2-Before",
            "Behavior1-Before",
            "Handler",
            "Behavior1-After",
            "Behavior2-After");

        // Verify both behaviors were called
        behavior1Mock.Verify(
            b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken),
            Times.Once);

        behavior2Mock.Verify(
            b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithBehaviorException_ShouldPropagateException()
    {
        // Arrange
        var request = new TestRequest("test");
        var cancellationToken = CancellationToken.None;
        var exception = new InvalidOperationException("Behavior failed");

        var behaviorMock = new Mock<IPipelineBehavior<TestRequest, TestResponse>>();
        behaviorMock
            .Setup(b => b.Handle(request, It.IsAny<RequestHandlerDelegate<TestResponse>>(), cancellationToken))
            .ThrowsAsync(exception);

        var behaviors = new[] { behaviorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Returns(behaviors);

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) => Task.FromResult(new TestResponse("response"));

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _pipelineService.Execute(request, handler, cancellationToken));

        thrownException.Should().BeSameAs(exception);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro na execução da pipeline para TestRequest")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithHandlerException_ShouldPropagateException()
    {
        // Arrange
        var request = new TestRequest("test");
        var cancellationToken = CancellationToken.None;
        var exception = new InvalidOperationException("Handler failed");

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Returns(Array.Empty<IPipelineBehavior<TestRequest, TestResponse>>());

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) => throw exception;

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _pipelineService.Execute(request, handler, cancellationToken));

        thrownException.Should().BeSameAs(exception);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro na execução da pipeline para TestRequest")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithServiceProviderException_ShouldUseEmptyBehaviors()
    {
        // Arrange
        var request = new TestRequest("test");
        var expectedResponse = new TestResponse("response");
        var cancellationToken = CancellationToken.None;

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>)))
            .Throws(new InvalidOperationException("Service provider error"));

        Func<TestRequest, CancellationToken, Task<TestResponse>> handler = (req, ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await _pipelineService.Execute(request, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResponse);

        // Verify warning logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Erro ao obter behaviors para TestRequest/TestResponse")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Test classes for testing
    public record TestRequest(string Value);
    public record TestResponse(string Value);
}

public class CommandPipelineTests
{
    private readonly Mock<PipelineService> _pipelineServiceMock;
    private readonly CommandPipeline _commandPipeline;

    public CommandPipelineTests()
    {
        _pipelineServiceMock = new Mock<PipelineService>(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<PipelineService>>());
        _commandPipeline = new CommandPipeline(_pipelineServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullPipelineService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CommandPipeline(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipelineService");
    }

    [Fact]
    public async Task Execute_WithCommand_ShouldCallPipelineService()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        Func<TestCommand, CancellationToken, Task<Result>> handler = (cmd, ct) => Task.FromResult(expectedResult);

        _pipelineServiceMock
            .Setup(p => p.Execute(command, handler, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _commandPipeline.Execute(command, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify pipeline service was called
        _pipelineServiceMock.Verify(
            p => p.Execute(command, handler, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Execute_WithCommandAndResult_ShouldCallPipelineService()
    {
        // Arrange
        var command = new TestCommandWithResult("test");
        var expectedResult = Result<string>.Success("test result");
        var cancellationToken = CancellationToken.None;

        Func<TestCommandWithResult, CancellationToken, Task<Result<string>>> handler = (cmd, ct) => Task.FromResult(expectedResult);

        _pipelineServiceMock
            .Setup(p => p.Execute(command, handler, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _commandPipeline.Execute(command, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify pipeline service was called
        _pipelineServiceMock.Verify(
            p => p.Execute(command, handler, cancellationToken),
            Times.Once);
    }

    // Test commands for testing
    public record TestCommand(string Value) : ICommand;
    public record TestCommandWithResult(string Value) : ICommand<string>;
}

public class QueryPipelineTests
{
    private readonly Mock<PipelineService> _pipelineServiceMock;
    private readonly QueryPipeline _queryPipeline;

    public QueryPipelineTests()
    {
        _pipelineServiceMock = new Mock<PipelineService>(Mock.Of<IServiceProvider>(), Mock.Of<ILogger<PipelineService>>());
        _queryPipeline = new QueryPipeline(_pipelineServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullPipelineService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new QueryPipeline(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipelineService");
    }

    [Fact]
    public async Task Execute_WithQuery_ShouldCallPipelineService()
    {
        // Arrange
        var query = new TestQuery("test");
        var expectedResult = Result<string>.Success("test result");
        var cancellationToken = CancellationToken.None;

        Func<TestQuery, CancellationToken, Task<Result<string>>> handler = (q, ct) => Task.FromResult(expectedResult);

        _pipelineServiceMock
            .Setup(p => p.Execute(query, handler, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _queryPipeline.Execute(query, handler, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify pipeline service was called
        _pipelineServiceMock.Verify(
            p => p.Execute(query, handler, cancellationToken),
            Times.Once);
    }

    // Test query for testing
    public record TestQuery(string Value) : IQuery<string>;
}
