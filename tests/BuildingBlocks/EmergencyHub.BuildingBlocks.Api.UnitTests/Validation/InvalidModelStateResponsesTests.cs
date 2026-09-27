using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Api.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Validation;

// InvalidModelStateResponseFactory: 모델 바인딩 오류(JSON 형식 · 형식 변환) → 400 ProblemDetails 1001(ADR-0016 "바인딩 오류").
// ActionContext를 직접 구성해 확인한다(HTTP 전 구간은 S03-T05).
public sealed class InvalidModelStateResponsesTests
{
    // ---- 성공 ----

    [Fact]
    public void Create_BindingErrors_Returns400ProblemWith1001AndFieldErrors()
    {
        var context = CreateActionContext();
        context.ModelState.AddModelError("$.email", "The JSON value could not be converted to System.String.");
        context.ModelState.AddModelError("NotificationChannels", "The value 'abc' is not valid.");

        var result = InvalidModelStateResponses.Create(context).Should().BeOfType<ObjectResult>().Subject;

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.ContentTypes.Should().Equal(ErrorProblemDetails.ContentType);
        var problem = result.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Extensions[ErrorProblemDetails.CodeExtension].Should().Be(CommonErrors.ValidationFailed.Code);
        problem.Extensions[ErrorProblemDetails.TraceIdExtension].Should().Be(HttpContexts.TraceIdentifier);
        var errors = FieldErrors(problem);
        // 키 순서는 ModelStateDictionary 열거 순서(접두사 트리)를 따르므로 순서는 단언하지 않는다.
        errors.Keys.Should().BeEquivalentTo(["email", "notificationChannels"]);
        errors.Values.SelectMany(items => items).Should().OnlyContain(item => item.Code == CommonErrors.ValidationFailed.Code);
    }

    [Fact]
    public void Create_SameFieldWithTwoErrors_KeepsBothEntries()
    {
        var context = CreateActionContext();
        context.ModelState.AddModelError("Email", "first");
        context.ModelState.AddModelError("Email", "second");

        var errors = FieldErrors(CreateProblem(context));

        errors["email"].Should().HaveCount(2);
    }

    [Fact]
    public void Create_LogsBindingFailureAtDebugWithFieldNamesOnly()
    {
        var collector = new FakeLogCollector();
        var context = CreateActionContext(collector);
        context.ModelState.AddModelError("$.email", "The value 'hong@example.com' is not valid.");

        InvalidModelStateResponses.Create(context);

        var record = collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(304);
        record.Level.Should().Be(LogLevel.Debug);
        record.Message.Should().Contain("email").And.NotContain("hong@example.com");
    }

    // ---- 실패 ----

    [Fact]
    public void Create_BindingErrorMessages_AreReplacedWithFixedMessage()
    {
        // 바인딩 오류 메시지에는 입력 값('hong@example.com')과 내부 형식 이름이 들어 있어 응답에 싣지 않는다.
        var context = CreateActionContext();
        context.ModelState.AddModelError("Email", "The value 'hong@example.com' is not valid for System.Int32.");
        context.ModelState.AddModelError("Age", new FormatException("Input string 'hong@example.com' was not in a correct format."), new EmptyModelMetadataProvider().GetMetadataForType(typeof(int)));

        var errors = FieldErrors(CreateProblem(context));

        errors.Values.SelectMany(items => items).Should().OnlyContain(item => item.Message == CommonErrors.ValidationFailed.Message);
    }

    [Fact]
    public void Create_NullContext_ThrowsArgumentNullException()
    {
        var act = () => InvalidModelStateResponses.Create(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData("$.email", "email")]
    [InlineData("$.items[0].name", "items[0].name")]
    [InlineData("$", "")]
    [InlineData("", "")]
    [InlineData("request.Email", "request.email")]
    [InlineData("employeeId", "employeeId")]
    public void Create_ModelStateKey_BecomesCamelCaseFieldKeyWithoutJsonRoot(string modelStateKey, string expectedKey)
    {
        var context = CreateActionContext();
        context.ModelState.AddModelError(modelStateKey, "invalid");

        FieldErrors(CreateProblem(context)).Keys.Should().ContainSingle().Which.Should().Be(expectedKey);
    }

    [Fact]
    public void Create_InvalidStateWithoutErrorEntries_StillReturnsOneObjectLevelEntry()
    {
        // 오류 항목 없이 무효로 표시된 경우에도 ValidationError(필드 1개 이상)를 만들 수 있어야 한다.
        var context = CreateActionContext();
        context.ModelState.SetModelValue("Name", rawValue: null, attemptedValue: null);
        context.ModelState["Name"]!.ValidationState = ModelValidationState.Invalid;

        var errors = FieldErrors(CreateProblem(context));

        errors.Should().ContainSingle().Which.Key.Should().BeEmpty();
    }

    private static ProblemDetails CreateProblem(ActionContext context) =>
        (ProblemDetails)((ObjectResult)InvalidModelStateResponses.Create(context)).Value!;

    private static IReadOnlyDictionary<string, ProblemFieldError[]> FieldErrors(ProblemDetails problem) =>
        (IReadOnlyDictionary<string, ProblemFieldError[]>)problem.Extensions[ErrorProblemDetails.ErrorsExtension]!;

    private static ActionContext CreateActionContext(FakeLogCollector? collector = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new FakeLoggerProvider(collector ?? new FakeLogCollector())));
        return new ActionContext(HttpContexts.Create(services: services.BuildServiceProvider()), new RouteData(), new ActionDescriptor());
    }
}
