namespace EmergencyHub.BuildingBlocks.Api.Errors;

/// <summary>
/// 검증 실패(400) · 상세 충돌(409) <c>ProblemDetails</c>의 <c>errors</c> 항목 하나입니다: <c>{ "code": 21004, "message": "..." }</c>
/// (원본: wiki/04-development/api-guidelines.md "에러 응답 포맷", ADR-0028).
/// </summary>
/// <param name="Code">항목별 정수 에러 코드(검증 실패 또는 충돌 유형).</param>
/// <param name="Message">사람이 읽는 설명.</param>
public sealed record ProblemFieldError(int Code, string Message);
