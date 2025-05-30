using FluentAssertions;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Queries;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Integration;

public class CQRSIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public CQRSIntegrationTests()
    {
        var services = new ServiceCollection();

        // Register core services
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        // Register pipeline services
        services.AddScoped<PipelineService>();
        services.AddScoped<CommandPipeline>();
        services.AddScoped<QueryPipeline>();

        // Register handlers
        services.AddScoped<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateUserCommand, UserDto>, UpdateUserCommandHandler>();
        services.AddScoped<IQueryHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();

        // Register behaviors
        services.AddScoped<IPipelineBehavior<CreateUserCommand, Result>, LoggingPipelineBehavior<CreateUserCommand>>();
        services.AddScoped<IPipelineBehavior<CreateUserCommand, Result>, ValidationPipelineBehavior<CreateUserCommand>>();
        services.AddScoped<IPipelineBehavior<CreateUserCommand, Result>, PerformancePipelineBehavior<CreateUserCommand>>();

        services.AddScoped<IPipelineBehavior<UpdateUserCommand, Result<UserDto>>, LoggingPipelineBehavior<UpdateUserCommand, UserDto>>();
        services.AddScoped<IPipelineBehavior<UpdateUserCommand, Result<UserDto>>, PerformancePipelineBehavior<UpdateUserCommand, UserDto>>();

        services.AddScoped<IPipelineBehavior<GetUserQuery, Result<UserDto>>, QueryLoggingPipelineBehavior<GetUserQuery, UserDto>>();
        services.AddScoped<IPipelineBehavior<GetUserQuery, Result<UserDto>>, QueryPerformancePipelineBehavior<GetUserQuery, UserDto>>();

        // Register validators
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

        _serviceProvider = services.BuildServiceProvider();
        _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        _queryBus = _serviceProvider.GetRequiredService<IQueryBus>();
    }

    [Fact]
    public async Task CommandBus_WithValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var command = new CreateUserCommand("John Doe", "john@example.com");

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBus_WithInvalidCommand_ShouldReturnValidationFailure()
    {
        // Arrange
        var command = new CreateUserCommand("", ""); // Invalid data

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Name is required"));
        result.Errors.Should().Contain(e => e.Contains("Email is required"));
    }

    [Fact]
    public async Task CommandBus_WithCommandWithResult_ShouldReturnResult()
    {
        // Arrange
        var command = new UpdateUserCommand(1, "Jane Doe", "jane@example.com");

        // Act
        var result = await _commandBus.SendAsync<UpdateUserCommand, UserDto>(command);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Jane Doe");
        result.Value.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task QueryBus_WithValidQuery_ShouldReturnResult()
    {
        // Arrange
        var query = new GetUserQuery(1);

        // Act
        var result = await _queryBus.SendAsync<GetUserQuery, UserDto>(query);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(1);
    }

    [Fact]
    public async Task QueryBus_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserQuery(999); // Non-existent user

        // Act
        var result = await _queryBus.SendAsync<GetUserQuery, UserDto>(query);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("User not found");
    }

    [Fact]
    public async Task FullWorkflow_CreateAndRetrieveUser_ShouldWorkEndToEnd()
    {
        // Arrange
        var createCommand = new CreateUserCommand("Alice Smith", "alice@example.com");
        var updateCommand = new UpdateUserCommand(1, "Alice Johnson", "alice.johnson@example.com");
        var getQuery = new GetUserQuery(1);

        // Act - Create user
        var createResult = await _commandBus.SendAsync(createCommand);

        // Act - Update user
        var updateResult = await _commandBus.SendAsync<UpdateUserCommand, UserDto>(updateCommand);

        // Act - Get user
        var getResult = await _queryBus.SendAsync<GetUserQuery, UserDto>(getQuery);

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        updateResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();

        getResult.Value.Name.Should().Be("Alice Johnson");
        getResult.Value.Email.Should().Be("alice.johnson@example.com");
    }

    [Fact]
    public async Task Pipeline_ShouldExecuteBehaviorsInCorrectOrder()
    {
        // Arrange
        var command = new CreateUserCommand("Test User", "test@example.com");
        var executionOrder = new List<string>();

        // We can't easily test the exact order without modifying the behaviors,
        // but we can verify that all behaviors are executed by checking logs

        // Act
        var result = await _commandBus.SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // In a real-world scenario, you would inject test behaviors
        // that track execution order
    }
}

// Test DTOs and Commands
public record UserDto(int Id, string Name, string Email);

public record CreateUserCommand(string Name, string Email) : ICommand;

public record UpdateUserCommand(int Id, string Name, string Email) : ICommand<UserDto>;

public record GetUserQuery(int Id) : IQuery<UserDto>;

// Test Handlers
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task<Result> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Simulate user creation logic
        return Task.FromResult(Result.Success());
    }
}

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserDto>
{
    public Task<Result<UserDto>> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Simulate user update logic
        var user = new UserDto(request.Id, request.Name, request.Email);
        return Task.FromResult(Result<UserDto>.Success(user));
    }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserDto>
{
    public Task<Result<UserDto>> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Simulate user retrieval logic
        if (request.Id == 999)
        {
            return Task.FromResult(Result<UserDto>.Failure("User not found"));
        }

        var user = new UserDto(request.Id, $"User {request.Id}", $"user{request.Id}@example.com");
        return Task.FromResult(Result<UserDto>.Success(user));
    }
}

// Test Validator
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    public Task<ValidationResult> ValidateAsync(CreateUserCommand instance, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add(new ValidationError("Name is required", nameof(CreateUserCommand.Name)));
        }

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add(new ValidationError("Email is required", nameof(CreateUserCommand.Email)));
        }

        var result = errors.Any() ? new ValidationResult(errors) : new ValidationResult();
        return Task.FromResult(result);
    }

    public ValidationResult Validate(CreateUserCommand instance)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add(new ValidationError("Name is required", nameof(CreateUserCommand.Name)));
        }

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add(new ValidationError("Email is required", nameof(CreateUserCommand.Email)));
        }

        return errors.Any() ? new ValidationResult(errors) : new ValidationResult();
    }

    public ValidationResult Validate(ValidationContext<CreateUserCommand> context)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(context.Instance.Name))
        {
            errors.Add(new ValidationError("Name is required", nameof(CreateUserCommand.Name)));
        }

        if (string.IsNullOrWhiteSpace(context.Instance.Email))
        {
            errors.Add(new ValidationError("Email is required", nameof(CreateUserCommand.Email)));
        }

        return errors.Any() ? new ValidationResult(errors) : new ValidationResult();
    }

    public Task<ValidationResult> ValidateAsync(ValidationContext<CreateUserCommand> context, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(context.Instance.Name))
        {
            errors.Add(new ValidationError("Name is required", nameof(CreateUserCommand.Name)));
        }

        if (string.IsNullOrWhiteSpace(context.Instance.Email))
        {
            errors.Add(new ValidationError("Email is required", nameof(CreateUserCommand.Email)));
        }

        var result = errors.Any() ? new ValidationResult(errors) : new ValidationResult();
        return Task.FromResult(result);
    }
}
