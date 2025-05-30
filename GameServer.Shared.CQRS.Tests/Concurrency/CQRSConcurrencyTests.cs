using System.Collections.Concurrent;
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

namespace GameServer.Shared.CQRS.Tests.Concurrency;

public class CQRSConcurrencyTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public CQRSConcurrencyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddCQRS(Assembly.GetExecutingAssembly());

        services.AddScoped<ICommandHandler<ConcurrentTestCommand>, ConcurrentTestCommandHandler>();
        services.AddScoped<ICommandHandler<ThreadSafeTestCommand, ConcurrentTestResult>, ThreadSafeTestCommandHandler>();
        services.AddScoped<IQueryHandler<ConcurrentTestQuery, ConcurrentTestResult>, ConcurrentTestQueryHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        _queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
    }

    [Fact]
    public async Task CommandBus_ConcurrentExecution_ShouldNotInterfere()
    {
        // Arrange
        const int concurrentRequests = 100;
        var tasks = new List<Task<Result>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var command = new ConcurrentTestCommand($"Command{i}", i);
            tasks.Add(_commandBus.SendAsync(command));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    [Fact]
    public async Task QueryBus_ConcurrentExecution_ShouldNotInterfere()
    {
        // Arrange
        const int concurrentRequests = 100;
        var tasks = new List<Task<Result<ConcurrentTestResult>>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var query = new ConcurrentTestQuery(i);
            tasks.Add(_queryBus.SendAsync<ConcurrentTestQuery, ConcurrentTestResult>(query));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Verify unique results
        var values = results.Select(r => r.Value!.Id).ToList();
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CQRS_WithSharedState_ShouldBeThreadSafe()
    {
        // Arrange
        const int concurrentRequests = 50;
        var tasks = new List<Task<Result<ConcurrentTestResult>>>();
        ThreadSafeTestCommandHandler.Counter = 0; // Reset static counter

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var command = new ThreadSafeTestCommand($"Command{i}");
            tasks.Add(_commandBus.SendAsync<ThreadSafeTestCommand, ConcurrentTestResult>(command));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Counter should be exactly the number of concurrent requests
        ThreadSafeTestCommandHandler.Counter.Should().Be(concurrentRequests);

        // All counter values in results should be unique and sequential
        var counterValues = results.Select(r => r.Value!.CounterValue).OrderBy(x => x).ToList();
        counterValues.Should().BeInAscendingOrder();
        counterValues.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ServiceProvider_Scoping_ShouldIsolateRequests()
    {
        // Arrange
        const int concurrentRequests = 20;
        var scopeIds = new ConcurrentBag<string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var task = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedCommandBus = scope.ServiceProvider.GetRequiredService<ICommandBus>();
                var command = new ConcurrentTestCommand($"Command{Thread.CurrentThread.ManagedThreadId}", Thread.CurrentThread.ManagedThreadId);

                // Add some delay to ensure overlap
                await Task.Delay(10);

                var result = await scopedCommandBus.SendAsync(command);
                result.IsSuccess.Should().BeTrue();

                scopeIds.Add(scope.GetHashCode().ToString());
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        scopeIds.Should().HaveCount(concurrentRequests);
        scopeIds.Distinct().Should().HaveCount(concurrentRequests); // All scopes should be unique
    }

    [Fact]
    public async Task PipelineService_ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        const int concurrentRequests = 50;
        var pipelineService = _serviceProvider.GetRequiredService<PipelineService>();
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<ConcurrentTestCommand>>();
        var tasks = new List<Task<Result>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var command = new ConcurrentTestCommand($"Command{i}", i);
            var task = pipelineService.Execute(command, handler.HandleAsync, CancellationToken.None);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    [Fact]
    public async Task CQRS_WithBehaviors_ShouldBeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddCQRS(Assembly.GetExecutingAssembly());

        services.AddScoped<ICommandHandler<ConcurrentTestCommand>, ConcurrentTestCommandHandler>();
        services.AddScoped<IPipelineBehavior<ConcurrentTestCommand, Result>, LoggingPipelineBehavior<ConcurrentTestCommand>>();
        services.AddScoped<IPipelineBehavior<ConcurrentTestCommand, Result>, PerformancePipelineBehavior<ConcurrentTestCommand>>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        const int concurrentRequests = 30;
        var tasks = new List<Task<Result>>();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            var command = new ConcurrentTestCommand($"Command{i}", i);
            tasks.Add(commandBus.SendAsync(command));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentRequests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
    }

    [Fact]
    public async Task CQRS_LongRunningOperations_ShouldNotBlock()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddCQRS(Assembly.GetExecutingAssembly());
        services.AddScoped<ICommandHandler<LongRunningTestCommand>, LongRunningTestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        const int requests = 5;
        var tasks = new List<Task<Result>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Start multiple long-running operations concurrently
        for (int i = 0; i < requests; i++)
        {
            var command = new LongRunningTestCommand($"LongCommand{i}");
            tasks.Add(commandBus.SendAsync(command));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(requests);
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        // Should complete faster than sequential execution
        // Each operation takes ~100ms, so 5 sequential would be ~500ms
        // Concurrent should be closer to ~100ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300);
    }

    [Fact]
    public async Task CQRS_CancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddCQRS(Assembly.GetExecutingAssembly());
        services.AddScoped<ICommandHandler<CancellableTestCommand>, CancellableTestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        var cts = new CancellationTokenSource();
        var command = new CancellableTestCommand("CancellableCommand");

        // Act
        var task = commandBus.SendAsync(command, cts.Token);

        // Cancel after a short delay
        _ = Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Assert
        var act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CQRS_ExceptionHandling_ShouldNotAffectOtherRequests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddCQRS(Assembly.GetExecutingAssembly());
        services.AddScoped<ICommandHandler<ExceptionTestCommand>, ExceptionTestCommandHandler>();
        services.AddScoped<ICommandHandler<ConcurrentTestCommand>, ConcurrentTestCommandHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

        var tasks = new List<Task<Result>>();

        // Act
        // Mix successful and exception-throwing commands
        for (int i = 0; i < 10; i++)
        {
            if (i % 3 == 0)
            {
                var exceptionCommand = new ExceptionTestCommand($"Exception{i}");
                tasks.Add(commandBus.SendAsync(exceptionCommand));
            }
            else
            {
                var normalCommand = new ConcurrentTestCommand($"Normal{i}", i);
                tasks.Add(commandBus.SendAsync(normalCommand));
            }
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => !r.IsSuccess).ToList();

        successfulResults.Should().NotBeEmpty();
        failedResults.Should().NotBeEmpty();

        // Failed results should not affect successful ones
        failedResults.Should().AllSatisfy(r => r.Errors.Should().Contain("Test exception"));
    }
}

// Test classes for concurrency tests
public record ConcurrentTestCommand(string Name, int Value) : ICommand;
public record ThreadSafeTestCommand(string Name) : ICommand<ConcurrentTestResult>;
public record ConcurrentTestQuery(int Id) : IQuery<ConcurrentTestResult>;
public record LongRunningTestCommand(string Name) : ICommand;
public record CancellableTestCommand(string Name) : ICommand;
public record ExceptionTestCommand(string Name) : ICommand;

public record ConcurrentTestResult(int Id, string Name, DateTime Timestamp, int CounterValue = 0);

public class ConcurrentTestCommandHandler : ICommandHandler<ConcurrentTestCommand>
{
    public async Task<Result> HandleAsync(ConcurrentTestCommand request, CancellationToken cancellationToken)
    {
        // Simulate some async work
        await Task.Delay(Random.Shared.Next(1, 10), cancellationToken);
        return Result.Success();
    }
}

public class ThreadSafeTestCommandHandler : ICommandHandler<ThreadSafeTestCommand, ConcurrentTestResult>
{
    public static int Counter = 0;

    public async Task<Result<ConcurrentTestResult>> HandleAsync(ThreadSafeTestCommand request, CancellationToken cancellationToken)
    {
        // Simulate some work
        await Task.Delay(Random.Shared.Next(1, 5), cancellationToken);

        // Thread-safe increment
        var currentValue = Interlocked.Increment(ref Counter);

        var result = new ConcurrentTestResult(
            currentValue,
            request.Name,
            DateTime.UtcNow,
            currentValue
        );

        return Result<ConcurrentTestResult>.Success(result);
    }
}

public class ConcurrentTestQueryHandler : IQueryHandler<ConcurrentTestQuery, ConcurrentTestResult>
{
    public async Task<Result<ConcurrentTestResult>> HandleAsync(ConcurrentTestQuery request, CancellationToken cancellationToken)
    {
        // Simulate some async work
        await Task.Delay(Random.Shared.Next(1, 10), cancellationToken);

        var result = new ConcurrentTestResult(
            request.Id,
            $"Query Result {request.Id}",
            DateTime.UtcNow
        );

        return Result<ConcurrentTestResult>.Success(result);
    }
}

public class LongRunningTestCommandHandler : ICommandHandler<LongRunningTestCommand>
{
    public async Task<Result> HandleAsync(LongRunningTestCommand request, CancellationToken cancellationToken)
    {
        // Simulate long-running operation
        await Task.Delay(100, cancellationToken);
        return Result.Success();
    }
}

public class CancellableTestCommandHandler : ICommandHandler<CancellableTestCommand>
{
    public async Task<Result> HandleAsync(CancellableTestCommand request, CancellationToken cancellationToken)
    {
        // Simulate work that can be cancelled
        await Task.Delay(200, cancellationToken);
        return Result.Success();
    }
}

public class ExceptionTestCommandHandler : ICommandHandler<ExceptionTestCommand>
{
    public Task<Result> HandleAsync(ExceptionTestCommand request, CancellationToken cancellationToken)
    {
        // Always throw an exception
        throw new InvalidOperationException($"Test exception for {request.Name}");
    }
}
