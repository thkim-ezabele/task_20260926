using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Errors;

// ADR-0028 "[Consumes] 불일치 415": ClientErrorResultFilter가 부르는 IClientErrorFactory를 감싸 415만 1005 ProblemDetails로 바꾼다.
// 그 밖의 클라이언트 오류 결과는 프레임워크 기본 팩토리(안쪽)에 그대로 맡긴다.
public sealed class UnsupportedMediaTypeClientErrorFactoryTests
{
    private readonly IClientErrorFactory _inner = Substitute.For<IClientErrorFactory>();

    // ---- 성공 ----

    [Fact]
    public void GetClientError_UnsupportedMediaTypeResult_Returns1005ProblemResultWithoutAskingInner()
    {
        var result = CreateFactory().GetClientError(CreateActionContext(), new UnsupportedMediaTypeResult());

        result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().Be(CommonErrors.UnsupportedMediaType);
        _inner.DidNotReceive().GetClientError(Arg.Any<ActionContext>(), Arg.Any<IClientErrorActionResult>());
    }

    // ---- 실패: 415가 아니면 안쪽 팩토리 결과 그대로 ----

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(413)]
    public void GetClientError_OtherStatus_DelegatesToInner(int status)
    {
        var context = CreateActionContext();
        var clientError = new StatusCodeResult(status);
        var expected = new ObjectResult("inner") { StatusCode = status };
        _inner.GetClientError(context, clientError).Returns(expected);

        var result = CreateFactory().GetClientError(context, clientError);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void GetClientError_NullClientError_ThrowsArgumentNullException()
    {
        var act = () => CreateFactory().GetClientError(CreateActionContext(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("clientError");
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new UnsupportedMediaTypeClientErrorFactory(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    // ---- 엣지 ----

    [Fact]
    public void GetClientError_Plain415StatusCodeResult_IsAlsoConverted()
    {
        // UnsupportedMediaTypeResult가 아니어도 상태 코드가 415면 같은 계약(1005)으로 응답한다.
        var result = CreateFactory().GetClientError(CreateActionContext(), new StatusCodeResult(415));

        result.Should().BeOfType<ErrorProblemResult>().Which.Error.Code.Should().Be(1005);
    }

    private UnsupportedMediaTypeClientErrorFactory CreateFactory() => new(_inner);

    private static ActionContext CreateActionContext() => new(HttpContexts.Create(), new RouteData(), new ActionDescriptor());
}
