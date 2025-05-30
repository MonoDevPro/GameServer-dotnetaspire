using FluentAssertions;
using GameServer.Shared.CQRS.Behaviors;
using GameServer.Shared.CQRS.Commands;
using GameServer.Shared.CQRS.Pipeline;
using GameServer.Shared.CQRS.Results;
using GameServer.Shared.CQRS.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Behaviors;

public class ValidationPipelineBehaviorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<ValidationPipelineBehavior<TestCommand>>> _loggerMock;
    private readonly ValidationPipelineBehavior<TestCommand> _behavior;

    public ValidationPipelineBehaviorTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<ValidationPipelineBehavior<TestCommand>>>();
        _behavior = new ValidationPipelineBehavior<TestCommand>(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ValidationPipelineBehavior<TestCommand>(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ValidationPipelineBehavior<TestCommand>(_serviceProviderMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldProceedToNext()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(Array.Empty<IValidator<TestCommand>>());

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify debug logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Nenhum validator encontrado para TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidValidation_ShouldProceedToNext()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult()); // Valid result

        var validators = new[] { validatorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify validation was called
        validatorMock.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken),
            Times.Once);

        // Verify debug logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executando 1 validators para TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validação concluída com sucesso para TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidValidation_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationToken = CancellationToken.None;

        var validationErrors = new[]
        {
            new ValidationError("Error 1", "Property1"),
            new ValidationError("Error 2", "Property2")
        };

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult(validationErrors));

        var validators = new[] { validatorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");

        // Verify validation was called
        validatorMock.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken),
            Times.Once);

        // Verify warning logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Comando TestCommand falhou na validação")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldRunAllValidators()
    {
        // Arrange
        var command = new TestCommand("test");
        var expectedResult = Result.Success();
        var cancellationToken = CancellationToken.None;

        var validator1Mock = new Mock<IValidator<TestCommand>>();
        var validator2Mock = new Mock<IValidator<TestCommand>>();

        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult());

        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { validator1Mock.Object, validator2Mock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);

        // Verify both validators were called
        validator1Mock.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken),
            Times.Once);

        validator2Mock.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken),
            Times.Once);

        // Verify debug logging shows correct count
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executando 2 validators para TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMixedValidationResults_ShouldCombineAllErrors()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationToken = CancellationToken.None;

        var validator1Mock = new Mock<IValidator<TestCommand>>();
        var validator2Mock = new Mock<IValidator<TestCommand>>();

        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult(new ValidationError("Validator1 Error", "Prop1")));

        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult(new ValidationError("Validator2 Error", "Prop2")));

        var validators = new[] { validator1Mock.Object, validator2Mock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Validator1 Error");
        result.Errors.Should().Contain("Validator2 Error");
    }

    [Fact]
    public async Task Handle_WithValidationException_ShouldPropagateException()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationToken = CancellationToken.None;
        var exception = new InvalidOperationException("Validation failed");

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ThrowsAsync(exception);

        var validators = new[] { validatorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _behavior.Handle(command, next, cancellationToken));

        thrownException.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task Handle_WithNullValidationErrors_ShouldFilterThemOut()
    {
        // Arrange
        var command = new TestCommand("test");
        var cancellationToken = CancellationToken.None;

        var validationErrors = new ValidationError?[]
        {
            new ValidationError("Valid Error", "Property1"),
            null,
            new ValidationError("Another Valid Error", "Property2")
        };

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), cancellationToken))
            .ReturnsAsync(new ValidationResult(validationErrors.Where(e => e != null)!));

        var validators = new[] { validatorMock.Object };

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IValidator<TestCommand>>)))
            .Returns(validators);

        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

        // Act
        var result = await _behavior.Handle(command, next, cancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Valid Error");
        result.Errors.Should().Contain("Another Valid Error");
    }

    // Test command for testing
    public record TestCommand(string Value) : ICommand;
}
