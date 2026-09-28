using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Results;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다.");

    [Fact]
    public void Success_Always_ReturnsSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success();

        var act = () => result.Error;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_WithError_ReturnsFailedResultWithError()
    {
        var result = Result.Failure(SampleError);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException()
    {
        var act = () => Result.Failure(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Fact]
    public void Failure_WithValidationError_KeepsDerivedErrorType()
    {
        var validationError = ValidationError.Create([FieldError.Create("Email", CommonErrors.ValidationFailed)]);

        var result = Result.Failure(validationError);

        result.Error.Should().BeOfType<ValidationError>().Which.Errors.Should().ContainSingle();
    }

    [Fact]
    public void ImplicitConversion_FromError_ReturnsFailure()
    {
        var result = Fail(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void ImplicitConversion_FromNullError_ThrowsArgumentNullException()
    {
        Error? error = null;

        var act = () => Fail(error!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenericSuccess_WithValue_ReturnsResultOfValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_WithError_ReturnsFailedResultOfT()
    {
        var result = Result.Failure<int>(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void GenericFailure_WithNullError_ThrowsArgumentNullException()
    {
        var act = () => Result.Failure<int>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    private static Result Fail(Error error) => error;
}
