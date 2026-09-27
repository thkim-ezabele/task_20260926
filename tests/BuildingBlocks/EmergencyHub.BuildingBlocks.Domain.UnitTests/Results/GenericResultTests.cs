using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Results;

// 대상: Result<T>
public sealed class GenericResultTests
{
    private static readonly Error SampleError = Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    [Fact]
    public void Value_OnSuccess_ReturnsValue()
    {
        var id = SampleId.New();

        var result = Result.Success(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<SampleId>(SampleError);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success("값");

        var act = () => result.Error;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Success_WithNullValue_ThrowsArgumentNullException()
    {
        var act = () => Result.Success<string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void Success_WithNullNullableValueType_ThrowsArgumentNullException()
    {
        var act = () => Result.Success<int?>(null);

        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Success_WithDefaultOrBoundaryValueTypeValue_ReturnsValue(int value)
    {
        var result = Result.Success(value);

        result.Value.Should().Be(value);
    }

    [Fact]
    public void Success_WithEmptyGuidValue_ReturnsValue()
    {
        var result = Result.Success(Guid.Empty);

        result.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ImplicitConversion_FromError_ReturnsFailure()
    {
        var result = ToResult(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void ImplicitConversion_FromNullError_ThrowsArgumentNullException()
    {
        Error? error = null;

        var act = () => ToResult(error!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_ReturnsSuccess()
    {
        var id = SampleId.New();

        var result = ToResult(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    public void ImplicitConversion_FromNullReferenceValue_ThrowsArgumentNullException()
    {
        string? value = null;

        var act = () => ToStringResult(value!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImplicitConversion_FromValueOfOtherFailedResult_PropagatesError()
    {
        var inner = Result.Failure<string>(SampleError);

        var result = Propagate(inner);

        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void ImplicitConversion_FromValueOfOtherSuccessfulResult_ReturnsValue()
    {
        var aggregate = new SampleAggregate(SampleId.New());
        var inner = Result.Success(aggregate);

        var result = ReturnId(inner);

        result.Value.Should().Be(aggregate.Id);
    }

    [Fact]
    public void GenericResult_Always_IsAssignableToResult()
    {
        Result result = Result.Failure<int>(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    private static Result<SampleId> ToResult(Error error) => error;

    private static Result<SampleId> ToResult(SampleId id) => id;

    private static Result<string> ToStringResult(string value) => value;

    private static Result<int> Propagate(Result<string> inner)
    {
        if (inner.IsFailure)
        {
            return inner.Error;
        }

        return inner.Value.Length;
    }

    // coding-conventions.md Handler 예시의 `return result.Value.Id;` 형태(BL-040).
    private static Result<SampleId> ReturnId(Result<SampleAggregate> result)
    {
        if (result.IsFailure)
        {
            return result.Error;
        }

        return result.Value.Id;
    }
}
