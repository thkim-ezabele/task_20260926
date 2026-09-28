using System.Diagnostics;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Errors;

// Result 실패(Error) → RFC 9457 ProblemDetails. 원본: api-guidelines "에러 응답 포맷", ADR-0016 · 0024.
// code는 JSON 숫자, traceId는 Activity.Current.TraceId(32 hex), 없으면 HttpContext.TraceIdentifier.
// Activity.Current는 AsyncLocal이라 테스트 메서드마다 독립이다.
public sealed class ErrorProblemDetailsTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static TheoryData<Error> ConflictErrors() =>
        new(CommonErrors.ConcurrencyConflict, CommonErrors.UniqueConstraintViolated, CommonErrors.DuplicateRequest, SampleErrors.DuplicateEmail);

    // ---- 성공 ----

    [Fact]
    public void Create_ServiceError_FillsStandardFieldsAndIntegerCode()
    {
        var problem = ErrorProblemDetails.Create(SampleErrors.DuplicateEmail, HttpContexts.Create());

        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Type.Should().Be("https://httpstatuses.io/409");
        problem.Title.Should().Be("Conflict");
        problem.Detail.Should().Be(SampleErrors.DuplicateEmail.Message);
        problem.Instance.Should().Be("/api/v1/employees");
        problem.Extensions[ErrorProblemDetails.CodeExtension].Should().Be(23001);
    }

    [Theory]
    [MemberData(nameof(ConflictErrors))]
    public void Create_ConflictErrors_Serialize409WithIntegerCodeAndNoErrors(Error error)
    {
        // 3001(xmin 충돌) · 3003(매핑 없는 23505) · 서비스 23xxx(매핑된 23505)는 모두 Conflict → 409(S02-T07 인계).
        // ADR-0028 하위 호환: 상세 없는 409에는 errors가 없고 표준 필드 · code · traceId만 있다.
        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        json.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
        json.GetProperty("code").GetInt32().Should().Be(error.Code);
        json.GetProperty("detail").GetString().Should().Be(error.Message);
        json.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("type", "title", "status", "detail", "instance", "code", "traceId");
    }

    [Fact]
    public void Create_ConflictError_Serializes409WithRepresentativeCodeAndRowKeyedErrorsInOrder()
    {
        // ADR-0028: 상세 Conflict 오류는 409 errors에 ValidationError와 같은 모양(키 camelCase 경로, 값 { code, message } 배열).
        var error = ConflictError.Create(
            SampleErrors.DuplicateEmail,
            [
                ConflictDetail.Create("Rows[7].Email", SampleErrors.DuplicateEmail),
                ConflictDetail.Create("Rows[3].Email", SampleErrors.DuplicateEmail),
                ConflictDetail.Create("Rows[7].Email", CommonErrors.UniqueConstraintViolated),
            ]);

        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create("/api/employee")));

        json.GetProperty("status").GetInt32().Should().Be(409);
        json.GetProperty("type").GetString().Should().Be("https://httpstatuses.io/409");
        json.GetProperty("title").GetString().Should().Be("Conflict");
        json.GetProperty("detail").GetString().Should().Be(SampleErrors.DuplicateEmail.Message);
        json.GetProperty("instance").GetString().Should().Be("/api/employee");
        json.GetProperty("code").GetInt32().Should().Be(23001);
        var errors = json.GetProperty("errors");
        errors.EnumerateObject().Select(property => property.Name).Should().Equal("rows[7].email", "rows[3].email");
        errors.GetProperty("rows[7].email").EnumerateArray().Select(item => item.GetProperty("code").GetInt32()).Should().Equal(23001, 3003);
        errors.GetProperty("rows[3].email")[0].GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
        errors.GetProperty("rows[3].email")[0].GetProperty("message").GetString().Should().Be(SampleErrors.DuplicateEmail.Message);
        errors.GetProperty("rows[3].email")[0].EnumerateObject().Select(property => property.Name).Should().Equal("code", "message");
    }

    [Fact]
    public void Create_ConflictErrorAndValidationErrorWithSamePaths_HaveSameErrorsShape()
    {
        // 클라이언트가 400 · 409 errors를 같은 코드로 처리할 수 있어야 한다(ADR-0028 결과).
        var validation = ValidationError.Create([FieldError.Create("Rows[3].Email", SampleErrors.InvalidEmail)]);
        var conflict = ConflictError.Create(SampleErrors.DuplicateEmail, [ConflictDetail.Create("Rows[3].Email", SampleErrors.DuplicateEmail)]);

        var validationErrors = Serialize(ErrorProblemDetails.Create(validation, HttpContexts.Create())).GetProperty("errors");
        var conflictErrors = Serialize(ErrorProblemDetails.Create(conflict, HttpContexts.Create())).GetProperty("errors");

        conflictErrors.EnumerateObject().Select(property => property.Name).Should().Equal(validationErrors.EnumerateObject().Select(property => property.Name));
        conflictErrors.GetProperty("rows[3].email")[0].EnumerateObject().Select(property => property.Name)
            .Should().Equal(validationErrors.GetProperty("rows[3].email")[0].EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void Create_MappedUniqueViolationErrors_DetailIsFixedMessageWithoutInputValue()
    {
        // ⑤ NFR-04: 23505 → 23001 / 3003 변환 결과의 detail은 오류의 고정 문구다. PostgresException.Detail(값 포함)은 들어가지 않는다.
        foreach (var error in new[] { SampleErrors.DuplicateEmail, CommonErrors.UniqueConstraintViolated })
        {
            var body = JsonSerializer.Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()), WebJson);

            body.Should().NotContain(UnconvertedDbFailures.SecretValue).And.NotContain("already exists").And.NotContain("ux_");
            Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create())).GetProperty("detail").GetString().Should().Be(error.Message);
        }
    }

    [Theory]
    [MemberData(nameof(RequestTransportErrors))]
    public void Create_PayloadTooLargeAndUnsupportedMediaType_Serialize413And415WithCommonCode(Error error, int status, string title)
    {
        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("status").GetInt32().Should().Be(status);
        json.GetProperty("type").GetString().Should().Be($"https://httpstatuses.io/{status}");
        json.GetProperty("title").GetString().Should().Be(title);
        json.GetProperty("code").GetInt32().Should().Be(error.Code);
        json.TryGetProperty("errors", out _).Should().BeFalse();
    }

    public static TheoryData<Error, int, string> RequestTransportErrors() => new()
    {
        { CommonErrors.PayloadTooLarge, StatusCodes.Status413PayloadTooLarge, "Payload Too Large" },
        { CommonErrors.UnsupportedMediaType, StatusCodes.Status415UnsupportedMediaType, "Unsupported Media Type" },
    };

    [Fact]
    public void Create_Serialized_HasCamelCaseStandardFieldsCodeNumberAndTraceIdString()
    {
        var json = Serialize(ErrorProblemDetails.Create(CommonErrors.NotFound, HttpContexts.Create()));

        json.GetProperty("type").GetString().Should().Be("https://httpstatuses.io/404");
        json.GetProperty("title").GetString().Should().Be("Not Found");
        json.GetProperty("status").GetInt32().Should().Be(404);
        json.GetProperty("detail").GetString().Should().Be(CommonErrors.NotFound.Message);
        json.GetProperty("code").GetInt32().Should().Be(2001);
        json.GetProperty("traceId").ValueKind.Should().Be(JsonValueKind.String);
        json.TryGetProperty("errors", out _).Should().BeFalse();
    }

    [Fact]
    public void Create_W3CActivity_UsesItsTraceIdAs32LowercaseHex()
    {
        using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();

        var problem = ErrorProblemDetails.Create(CommonErrors.NotFound, HttpContexts.Create());

        var traceId = problem.Extensions[ErrorProblemDetails.TraceIdExtension].Should().BeOfType<string>().Subject;
        traceId.Should().Be(activity.TraceId.ToHexString());
        traceId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public void Create_NoActivity_UsesTraceIdentifier()
    {
        Activity.Current = null;

        var problem = ErrorProblemDetails.Create(CommonErrors.NotFound, HttpContexts.Create());

        problem.Extensions[ErrorProblemDetails.TraceIdExtension].Should().Be(HttpContexts.TraceIdentifier);
    }

    [Fact]
    public void Create_ValidationError_PutsFieldErrorsUnderCamelCaseKeysInOrder()
    {
        var error = ValidationError.Create(
        [
            FieldError.Create("Email", SampleErrors.InvalidEmail),
            FieldError.Create("NotificationChannels", CommonErrors.InvalidCode),
            FieldError.Create("Email", CommonErrors.ValidationFailed),
        ]);

        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("status").GetInt32().Should().Be(400);
        json.GetProperty("code").GetInt32().Should().Be(1001);
        var errors = json.GetProperty("errors");
        errors.EnumerateObject().Select(property => property.Name).Should().Equal("email", "notificationChannels");
        errors.GetProperty("email").EnumerateArray().Select(item => item.GetProperty("code").GetInt32()).Should().Equal(21001, 1001);
        errors.GetProperty("email")[0].GetProperty("message").GetString().Should().Be(SampleErrors.InvalidEmail.Message);
        errors.GetProperty("notificationChannels")[0].GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number);
        errors.GetProperty("notificationChannels")[0].GetProperty("code").GetInt32().Should().Be(1002);
    }

    [Fact]
    public void Create_NonValidationError_HasNoErrorsExtension()
    {
        var problem = ErrorProblemDetails.Create(SampleErrors.InvalidTransition, HttpContexts.Create());

        problem.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem.Extensions.Should().NotContainKey(ErrorProblemDetails.ErrorsExtension);
    }

    // ---- 실패 ----

    [Fact]
    public void Create_NullError_ThrowsArgumentNullException()
    {
        var act = () => ErrorProblemDetails.Create(null!, HttpContexts.Create());

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Fact]
    public void Create_NullHttpContext_ThrowsArgumentNullException()
    {
        var act = () => ErrorProblemDetails.Create(CommonErrors.NotFound, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpContext");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData("Items[0].Name", "items[0].name")]
    [InlineData("Address.ZipCode", "address.zipCode")]
    [InlineData("URLValue", "urlValue")]
    [InlineData("email", "email")]
    [InlineData("", "")]
    public void Create_ValidationErrorPropertyPath_ConvertsEachSegmentToCamelCase(string propertyName, string expectedKey)
    {
        var error = ValidationError.Create([FieldError.Create(propertyName, SampleErrors.NameRequired)]);

        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("errors").EnumerateObject().Should().ContainSingle().Which.Name.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("Rows[3].Email", "rows[3].email")]
    [InlineData("Rows[1000].Email", "rows[1000].email")]
    [InlineData("", "")]
    public void Create_ConflictErrorPropertyPath_ConvertsEachSegmentToCamelCase(string propertyName, string expectedKey)
    {
        var error = ConflictError.Create(SampleErrors.DuplicateEmail, [ConflictDetail.Create(propertyName, SampleErrors.DuplicateEmail)]);

        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("errors").EnumerateObject().Should().ContainSingle().Which.Name.Should().Be(expectedKey);
    }

    [Fact]
    public void Create_HierarchicalActivity_FallsBackToTraceIdentifier()
    {
        // W3C 형식이 아닌 Activity는 TraceId가 비어 있다(default). 32 hex가 아니면 쓰지 않는다.
        using var activity = new Activity("legacy").SetIdFormat(ActivityIdFormat.Hierarchical).Start();

        var problem = ErrorProblemDetails.Create(CommonErrors.NotFound, HttpContexts.Create());

        problem.Extensions[ErrorProblemDetails.TraceIdExtension].Should().Be(HttpContexts.TraceIdentifier);
    }

    [Fact]
    public void Create_QueryString_IsNotIncludedInInstance()
    {
        // 쿼리 문자열에는 검색어 같은 개인정보가 들어갈 수 있다. instance는 경로만 담는다.
        var context = HttpContexts.Create("/api/v1/employees");
        context.Request.PathBase = "/employee";
        context.Request.QueryString = new QueryString("?email=hong@example.com");

        var problem = ErrorProblemDetails.Create(CommonErrors.NotFound, context);

        problem.Instance.Should().Be("/employee/api/v1/employees");
    }

    [Fact]
    public void Create_UnicodeMessage_IsKeptInDetail()
    {
        var error = Error.BusinessRule(24009, "종료된 긴급 상황은 바꿀 수 없습니다 😀");

        var json = Serialize(ErrorProblemDetails.Create(error, HttpContexts.Create()));

        json.GetProperty("detail").GetString().Should().Be(error.Message);
    }

    private static JsonElement Serialize(ProblemDetails problem)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(problem, WebJson));
        return document.RootElement.Clone();
    }
}
