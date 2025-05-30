using FluentAssertions;
using GameServer.Shared.CQRS.Results;
using Xunit;

namespace GameServer.Shared.CQRS.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithSingleError_ShouldCreateFailureResult()
    {
        // Arrange
        const string error = "Test error";

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void Failure_WithMultipleErrors_ShouldCreateFailureResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result.Failure(errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(errors);
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailureResultWithExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.Failure(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be("Test exception");
    }

    [Fact]
    public void Constructor_WithSuccessAndErrors_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var act = () => new TestableResult(true, new[] { "error" });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Um resultado de sucesso não pode conter erros.");
    }

    [Fact]
    public void Constructor_WithFailureAndNoErrors_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var act = () => new TestableResult(false, Array.Empty<string>());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Um resultado de falha deve conter pelo menos um erro.");
    }

    [Fact]
    public void Constructor_WithEmptyAndWhitespaceErrors_ShouldFilterThemOut()
    {
        // Arrange
        var errors = new[] { "Valid error", "", "   ", "\t", "Another valid error" };

        // Act
        var result = new TestableResult(false, errors);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Valid error");
        result.Errors.Should().Contain("Another valid error");
    }

    [Fact]
    public void OnSuccess_WithSuccessfulResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void OnSuccess_WithFailureResult_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.Failure("error");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        executed.Should().BeFalse();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void OnFailure_WithFailureResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Failure("test error");
        IReadOnlyList<string>? capturedErrors = null;

        // Act
        var returnedResult = result.OnFailure(errors => capturedErrors = errors);

        // Assert
        capturedErrors.Should().NotBeNull();
        capturedErrors.Should().ContainSingle().Which.Should().Be("test error");
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void OnFailure_WithSuccessfulResult_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returnedResult = result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
        returnedResult.Should().BeSameAs(result);
    }

    [Fact]
    public void Failure_WithNullErrors_ShouldCreateEmptyErrorsList()
    {
        // Act
        var result = Result.Failure((IEnumerable<string>)null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // Helper class to test protected constructor
    private class TestableResult : Result
    {
        public TestableResult(bool isSuccess, IEnumerable<string>? errors = null)
            : base(isSuccess, errors)
        {
        }
    }
}
