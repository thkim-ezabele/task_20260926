using System.Globalization;
using System.Net;
using System.Text.Json;

namespace EmergencyHub.Employee.IntegrationTests.Http;

/// <summary>
/// HTTP 인수 테스트의 ProblemDetails 응답 읽기 · 형태 단언입니다(wiki/05-api/employee-api.md 실패 응답 표, api-guidelines "에러 응답 포맷").
/// </summary>
/// <remarks>
/// 형태: <c>application/problem+json</c>, 키는 <c>type</c> · <c>title</c> · <c>status</c> · <c>detail</c> · <c>instance</c> · <c>code</c> · <c>traceId</c>
/// (+ 검증 실패 · 상세 충돌이면 <c>errors</c>)뿐이고, <c>type</c>은 <c>https://httpstatuses.io/{status}</c>, <c>code</c>는 JSON 숫자, <c>traceId</c>는 32자리 소문자 16진수입니다.
/// </remarks>
internal static class HttpProblem
{
    public const string ContentType = "application/problem+json";

    private static readonly string[] BaseKeys = ["type", "title", "status", "detail", "instance", "code", "traceId"];

    /// <summary>응답이 ProblemDetails 형태인지 확인하고 본문을 돌려줍니다.</summary>
    /// <param name="response">응답.</param>
    /// <param name="status">기대 HTTP 상태.</param>
    /// <param name="code">기대 정수 에러 코드.</param>
    /// <param name="detail">기대 고정 문구(에러 정의의 메시지).</param>
    /// <param name="instance">기대 <c>instance</c>(요청 경로, 쿼리 문자열 없음).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <param name="hasErrors">
    /// <c>errors</c> 확장이 있어야 하는지입니다. <see langword="null"/>이면 <c>code == 1001</c>일 때만 있습니다(상세 Conflict 409는 <see langword="true"/>로 지정, ADR-0028).
    /// </param>
    /// <returns>본문 루트(복제본).</returns>
    public static async Task<JsonElement> ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        int code,
        string detail,
        string instance,
        CancellationToken cancellationToken,
        bool? hasErrors = null)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ContentType);
        // MVC 결과(400 · 404 · 409)는 charset=utf-8을 붙이고, 전역 예외 처리기(WriteAsJsonAsync, 500)는 붙이지 않는다. JSON 미디어 형식의 기본 인코딩은 UTF-8이고
        // 아래 detail(한국어) 비교가 본문이 UTF-8임을 확인한다.
        response.Content.Headers.ContentType.CharSet.Should().BeOneOf([null, "utf-8"], "JSON 기본 인코딩은 UTF-8");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement.Clone();

        var statusNumber = (int)status;
        var keys = root.EnumerateObject().Select(property => property.Name).ToArray();
        var expectedKeys = hasErrors ?? code == 1001 ? [.. BaseKeys, "errors"] : BaseKeys;
        keys.Should().Equal(expectedKeys, "ProblemDetails 키와 순서는 문서 예시와 같다(검증 실패 · 상세 충돌만 errors를 더한다)");

        root.GetProperty("type").GetString().Should().Be("https://httpstatuses.io/" + statusNumber.ToString(CultureInfo.InvariantCulture));
        root.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("status").GetInt32().Should().Be(statusNumber);
        root.GetProperty("detail").GetString().Should().Be(detail);
        root.GetProperty("instance").GetString().Should().Be(instance);
        root.GetProperty("code").ValueKind.Should().Be(JsonValueKind.Number, "코드는 정수(ADR-0008)");
        root.GetProperty("code").GetInt32().Should().Be(code);
        root.GetProperty("traceId").GetString().Should().MatchRegex("^[0-9a-f]{32}$");

        return root;
    }

    /// <summary><c>errors</c>의 항목을 (필드 키, 코드) 목록으로 펼칩니다(키 · 항목 순서 유지).</summary>
    /// <param name="problem">ProblemDetails 본문.</param>
    /// <returns>(필드 키, 코드) 목록.</returns>
    public static IReadOnlyList<(string Field, int Code)> FieldCodes(this JsonElement problem) =>
        [.. problem.GetProperty("errors").EnumerateObject()
            .SelectMany(field => field.Value.EnumerateArray().Select(item => (field.Name, item.GetProperty("code").GetInt32())))];
}
