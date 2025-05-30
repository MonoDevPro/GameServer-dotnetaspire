using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
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

namespace GameServer.Shared.CQRS.Tests.Performance;

[MemoryDiagnoser]
[SimpleJob]
[RPlotExporter]
public class CQRSPerformanceBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private ICommandBus _commandBus = null!;
    private IQueryBus _queryBus = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Register CQRS services
        services.AddCQRS(Assembly.GetExecutingAssembly());

        // Register handlers
        services.AddScoped<ICommandHandler<BenchmarkCommand>, BenchmarkCommandHandler>();
        services.AddScoped<ICommandHandler<BenchmarkCommandWithResult, BenchmarkResult>, BenchmarkCommandWithResultHandler>();
        services.AddScoped<IQueryHandler<BenchmarkQuery, BenchmarkResult>, BenchmarkQueryHandler>();

        // Register behaviors
        services.AddScoped<IPipelineBehavior<BenchmarkCommand, Result>, LoggingPipelineBehavior<BenchmarkCommand>>();
        services.AddScoped<IPipelineBehavior<BenchmarkCommand, Result>, PerformancePipelineBehavior<BenchmarkCommand>>();
        services.AddScoped<IPipelineBehavior<BenchmarkCommandWithResult, Result<BenchmarkResult>>, LoggingPipelineBehavior<BenchmarkCommandWithResult, BenchmarkResult>>();
        services.AddScoped<IPipelineBehavior<BenchmarkCommandWithResult, Result<BenchmarkResult>>, PerformancePipelineBehavior<BenchmarkCommandWithResult, BenchmarkResult>>();
        services.AddScoped<IPipelineBehavior<BenchmarkQuery, Result<BenchmarkResult>>, QueryLoggingPipelineBehavior<BenchmarkQuery, BenchmarkResult>>();
        services.AddScoped<IPipelineBehavior<BenchmarkQuery, Result<BenchmarkResult>>, QueryPerformancePipelineBehavior<BenchmarkQuery, BenchmarkResult>>();

        _serviceProvider = services.BuildServiceProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        _queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
    }

    [Benchmark]
    public async Task<Result> CommandBus_ExecuteSimpleCommand()
    {
        var command = new BenchmarkCommand("Test");
        return await _commandBus.SendAsync(command);
    }

    [Benchmark]
    public async Task<Result<BenchmarkResult>> CommandBus_ExecuteCommandWithResult()
    {
        var command = new BenchmarkCommandWithResult("Test", 42);
        return await _commandBus.SendAsync<BenchmarkCommandWithResult, BenchmarkResult>(command);
    }

    [Benchmark]
    public async Task<Result<BenchmarkResult>> QueryBus_ExecuteQuery()
    {
        var query = new BenchmarkQuery(1);
        return await _queryBus.SendAsync<BenchmarkQuery, BenchmarkResult>(query);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task CommandBus_ExecuteMultipleCommands(int count)
    {
        var tasks = new List<Task<Result>>();
        for (int i = 0; i < count; i++)
        {
            var command = new BenchmarkCommand($"Test{i}");
            tasks.Add(_commandBus.SendAsync(command));
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task QueryBus_ExecuteMultipleQueries(int count)
    {
        var tasks = new List<Task<Result<BenchmarkResult>>>();
        for (int i = 0; i < count; i++)
        {
            var query = new BenchmarkQuery(i);
            tasks.Add(_queryBus.SendAsync<BenchmarkQuery, BenchmarkResult>(query));
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task PipelineService_DirectExecution()
    {
        var pipelineService = _serviceProvider.GetRequiredService<PipelineService>();
        var command = new BenchmarkCommand("Test");
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<BenchmarkCommand>>();

        await pipelineService.Execute(command, handler.HandleAsync, CancellationToken.None);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

public class CQRSPerformanceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public CQRSPerformanceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddCQRS(Assembly.GetExecutingAssembly());

        services.AddScoped<ICommandHandler<BenchmarkCommand>, BenchmarkCommandHandler>();
        services.AddScoped<ICommandHandler<BenchmarkCommandWithResult, BenchmarkResult>, BenchmarkCommandWithResultHandler>();
        services.AddScoped<IQueryHandler<BenchmarkQuery, BenchmarkResult>, BenchmarkQueryHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        _queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
    }

    [Fact]
    public async Task CommandBus_Performance_ShouldExecuteWithinAcceptableTime()
    {
        // Arrange
        var command = new BenchmarkCommand("Test");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _commandBus.SendAsync(command);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should execute within 100ms
    }

    [Fact]
    public async Task QueryBus_Performance_ShouldExecuteWithinAcceptableTime()
    {
        // Arrange
        var query = new BenchmarkQuery(1);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _queryBus.SendAsync<BenchmarkQuery, BenchmarkResult>(query);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task CommandBus_ConcurrentExecution_ShouldHandleLoad()
    {
        // Arrange
        const int concurrentCommands = 100;
        var tasks = new List<Task<Result>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < concurrentCommands; i++)
        {
            var command = new BenchmarkCommand($"Test{i}");
            tasks.Add(_commandBus.SendAsync(command));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds for 100 commands
    }

    [Fact]
    public async Task QueryBus_ConcurrentExecution_ShouldHandleLoad()
    {
        // Arrange
        const int concurrentQueries = 100;
        var tasks = new List<Task<Result<BenchmarkResult>>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < concurrentQueries; i++)
        {
            var query = new BenchmarkQuery(i);
            tasks.Add(_queryBus.SendAsync<BenchmarkQuery, BenchmarkResult>(query));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task CQRS_MemoryUsage_ShouldNotLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var command = new BenchmarkCommand($"Test{i}");
            await _commandBus.SendAsync(command);

            var query = new BenchmarkQuery(i);
            await _queryBus.SendAsync<BenchmarkQuery, BenchmarkResult>(query);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(10_000_000); // Less than 10MB increase
    }

    [Fact]
    public async Task PipelineService_Performance_ShouldBeEfficient()
    {
        // Arrange
        var pipelineService = _serviceProvider.GetRequiredService<PipelineService>();
        var command = new BenchmarkCommand("Test");
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<BenchmarkCommand>>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await pipelineService.Execute(command, handler.HandleAsync, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    [Fact]
    public void ServiceProvider_Resolution_ShouldBeEfficient()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            using var scope = _serviceProvider.CreateScope();
            var commandBus = scope.ServiceProvider.GetRequiredService<ICommandBus>();
            var queryBus = scope.ServiceProvider.GetRequiredService<IQueryBus>();
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // 1 second for 1000 resolutions
    }

    [Fact]
    public void RunBenchmarks()
    {
        // This test method can be used to run benchmarks manually
        // Uncomment the line below to run benchmarks
        // BenchmarkRunner.Run<CQRSPerformanceBenchmarks>();
    }
}

// Benchmark DTOs and handlers
public record BenchmarkCommand(string Name) : ICommand;
public record BenchmarkCommandWithResult(string Name, int Value) : ICommand<BenchmarkResult>;
public record BenchmarkQuery(int Id) : IQuery<BenchmarkResult>;

public record BenchmarkResult(int Id, string Name, DateTime Timestamp);

public class BenchmarkCommandHandler : ICommandHandler<BenchmarkCommand>
{
    public Task<Result> HandleAsync(BenchmarkCommand request, CancellationToken cancellationToken)
    {
        // Simulate some work
        Task.Delay(1, cancellationToken);
        return Task.FromResult(Result.Success());
    }
}

public class BenchmarkCommandWithResultHandler : ICommandHandler<BenchmarkCommandWithResult, BenchmarkResult>
{
    public Task<Result<BenchmarkResult>> HandleAsync(BenchmarkCommandWithResult request, CancellationToken cancellationToken)
    {
        // Simulate some work
        Task.Delay(1, cancellationToken);
        var result = new BenchmarkResult(request.Value, request.Name, DateTime.UtcNow);
        return Task.FromResult(Result<BenchmarkResult>.Success(result));
    }
}

public class BenchmarkQueryHandler : IQueryHandler<BenchmarkQuery, BenchmarkResult>
{
    public Task<Result<BenchmarkResult>> HandleAsync(BenchmarkQuery request, CancellationToken cancellationToken)
    {
        // Simulate some work
        Task.Delay(1, cancellationToken);
        var result = new BenchmarkResult(request.Id, $"Query Result {request.Id}", DateTime.UtcNow);
        return Task.FromResult(Result<BenchmarkResult>.Success(result));
    }
}
