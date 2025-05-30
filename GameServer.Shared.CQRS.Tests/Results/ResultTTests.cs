using FluentAssertions;
using GameServer.Shared.CQRS.Results;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Results;

public class ResultTTests
{
    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Success_WithNullValue_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var act = () => Result<string>.Success(null!);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Um resultado de sucesso não pode ter valor nulo.");
    }

    [Fact]
    public void Failure_WithSingleError_ShouldCreateFailureResult()
    {
        // Arrange
        const string error = "Test error";

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.HasValue.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldCreateFailureResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result<string>.Failure(errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.HasValue.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(errors);
    }

    [Fact]
    public void Value_WithFailureResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result<string>.Failure("error");

        // Act & Assert
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Não é possível acessar o valor de um resultado de falha. Erros: error");
    }

    [Fact]
    public void Value_WithMultipleErrors_ShouldThrowInvalidOperationExceptionWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result<string>.Failure(errors);

        // Act & Assert
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Não é possível acessar o valor de um resultado de falha. Erros: Error 1, Error 2");
    }

    [Fact]
    public void HasValue_WithSuccessfulResultAndNonNullValue_ShouldReturnTrue()
    {
        // Arrange
        var result = Result<string>.Success("test");

        // Act & Assert
        result.HasValue.Should().BeTrue();
    }

    [Fact]
    public void HasValue_WithFailureResult_ShouldReturnFalse()
    {
        // Arrange
        var result = Result<string>.Failure("error");

        // Act & Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToBaseResult_ShouldWorkCorrectly()
    {
        // Arrange
        var genericResult = Result<string>.Success("test");

        // Act
        Result baseResult = genericResult;

        // Assert
        baseResult.IsSuccess.Should().BeTrue();
        baseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversion_FailureToBaseResult_ShouldWorkCorrectly()
    {
        // Arrange
        var genericResult = Result<string>.Failure("error");

        // Act
        Result baseResult = genericResult;

        // Assert
        baseResult.IsFailure.Should().BeTrue();
        baseResult.Errors.Should().ContainSingle().Which.Should().Be("error");
    }

    [Fact]
    public void OnSuccess_WithSuccessfulGenericResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<string>.Success("test");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void OnFailure_WithFailureGenericResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result<string>.Failure("test error");
        IReadOnlyList<string>? capturedErrors = null;

        // Act
        var returnedResult = result.OnFailure(errors => capturedErrors = errors);

        // Assert
        capturedErrors.Should().NotBeNull();
        capturedErrors.Should().ContainSingle().Which.Should().Be("test error");
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void Success_WithComplexObject_ShouldWorkCorrectly()
    {
        // Arrange
        var complexObject = new TestComplexObject { Id = 1, Name = "Test" };

        // Act
        var result = Result<TestComplexObject>.Success(complexObject);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.HasValue.Should().BeTrue();
        result.Value.Should().BeSameAs(complexObject);
        result.Value.Id.Should().Be(1);
        result.Value.Name.Should().Be("Test");
    }

    [Fact]
    public void Success_WithValueType_ShouldWorkCorrectly()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_WithEmptyErrorsList_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => Result<string>.Failure(Array.Empty<string>());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Um resultado de falha deve conter pelo menos um erro.");
    }

    [Fact]
    public void Failure_WithNullErrors_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => Result<string>.Failure((IEnumerable<string>?)null!);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Um resultado de falha deve conter pelo menos um erro.");
    }

    // Test helper class
    private class TestComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
