using FluentAssertions;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Queries;

public class QueryBusTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<QueryBus>> _loggerMock;
    private readonly Mock<QueryPipeline> _pipelineMock;
    private readonly QueryBus _queryBus;

    public QueryBusTests()
    {
        var services = new ServiceCollection();
        _loggerMock = new Mock<ILogger<QueryBus>>();
        _pipelineMock = new Mock<QueryPipeline>();

        // Registrar handlers de teste
        services.AddTransient<IQueryHandler<TestQuery, string>, TestQueryHandler>();
        services.AddTransient<IQueryHandler<TestComplexQuery, TestQueryResult>, TestComplexQueryHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _queryBus = new QueryBus(_serviceProvider, _loggerMock.Object, _pipelineMock.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidQuery_ShouldReturnExpectedResult()
    {
        // Arrange
        var query = new TestQuery("test");
        var expectedValue = "result: test";

        _pipelineMock
            .Setup(p => p.Execute<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestQuery, Func<TestQuery, CancellationToken, Task<Result<string>>>, CancellationToken>(
                async (qry, handler, token) => await handler(qry, token));

        // Act
        var result = await _queryBus.SendAsync<TestQuery, string>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
        _pipelineMock.Verify(p => p.Execute<TestQuery, string>(
            It.Is<TestQuery>(q => q.Value == "test"),
            It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithComplexQuery_ShouldReturnComplexResult()
    {
        // Arrange
        var query = new TestComplexQuery("complex", 42);
        var expectedResult = new TestQueryResult("complex", 42, DateTime.UtcNow);

        _pipelineMock
            .Setup(p => p.Execute<TestComplexQuery, TestQueryResult>(
                It.IsAny<TestComplexQuery>(),
                It.IsAny<Func<TestComplexQuery, CancellationToken, Task<Result<TestQueryResult>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<TestComplexQuery, Func<TestComplexQuery, CancellationToken, Task<Result<TestQueryResult>>>, CancellationToken>(
                async (qry, handler, token) => await handler(qry, token));

        // Act
        var result = await _queryBus.SendAsync<TestComplexQuery, TestQueryResult>(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("complex");
        result.Value.Count.Should().Be(42);
    }

    [Fact]
    public async Task SendAsync_WithUnregisteredQuery_ShouldReturnFailure()
    {
        // Arrange
        var query = new UnregisteredQuery();

        _pipelineMock
            .Setup(p => p.Execute<UnregisteredQuery, string>(
                It.IsAny<UnregisteredQuery>(),
                It.IsAny<Func<UnregisteredQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<UnregisteredQuery, Func<UnregisteredQuery, CancellationToken, Task<Result<string>>>, CancellationToken>(
                async (qry, handler, token) => await handler(qry, token));

        // Act
        var result = await _queryBus.SendAsync<UnregisteredQuery, string>(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Contains("Nenhum handler encontrado"));
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldHandleCancellation()
    {
        // Arrange
        var query = new TestQuery("test");
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _pipelineMock
            .Setup(p => p.Execute<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _queryBus.SendAsync<TestQuery, string>(query, cancellationTokenSource.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Operação cancelada");
    }

    [Fact]
    public async Task SendAsync_WithExceptionInPipeline_ShouldReturnFailureResult()
    {
        // Arrange
        var query = new TestQuery("test");
        var exception = new InvalidOperationException("Test exception");

        _pipelineMock
            .Setup(p => p.Execute<TestQuery, string>(
                It.IsAny<TestQuery>(),
                It.IsAny<Func<TestQuery, CancellationToken, Task<Result<string>>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _queryBus.SendAsync<TestQuery, string>(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Contains("Erro interno"));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBus(null!, _loggerMock.Object, _pipelineMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBus(_serviceProvider, null!, _pipelineMock.Object));
    }

    [Fact]
    public void Constructor_WithNullPipeline_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBus(_serviceProvider, _loggerMock.Object, null!));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    // Test Query Classes
    public record TestQuery(string Value) : IQuery<string>;
    public record TestComplexQuery(string Name, int Count) : IQuery<TestQueryResult>;
    public record UnregisteredQuery : IQuery<string>;

    // Test Result Classes
    public record TestQueryResult(string Name, int Count, DateTime Timestamp);

    // Test Handlers
    public class TestQueryHandler : IQueryHandler<TestQuery, string>
    {
        public Task<Result<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            var result = $"result: {query.Value}";
            return Task.FromResult(Result<string>.Success(result));
        }
    }

    public class TestComplexQueryHandler : IQueryHandler<TestComplexQuery, TestQueryResult>
    {
        public Task<Result<TestQueryResult>> HandleAsync(TestComplexQuery query, CancellationToken cancellationToken = default)
        {
            var result = new TestQueryResult(query.Name, query.Count, DateTime.UtcNow);
            return Task.FromResult(Result<TestQueryResult>.Success(result));
        }
    }
}
