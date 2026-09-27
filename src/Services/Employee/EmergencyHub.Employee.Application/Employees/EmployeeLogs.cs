using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.Application.Employees;

/// <summary>
/// Employee Application의 로그 정의입니다. 이벤트 ID는 Employee 범위 20001 ~ 20999를 씁니다
/// (원본: wiki/05-api/error-codes.md "Employee 로그 이벤트").
/// </summary>
/// <remarks>개인정보(이름 · 이메일)는 남기지 않고 직원 ID만 남깁니다(logging-observability "개인정보 · 보안").</remarks>
internal static partial class EmployeeLogs
{
    /// <remarks>
    /// Handler 안에서 남기므로 커밋 전 시점입니다. 커밋이 실패하면(예: 경합 23505 → 23001) 로깅 데코레이터가 같은 요청의 실패(102)를 뒤이어 남깁니다.
    /// </remarks>
    [LoggerMessage(EventId = 20001, Level = LogLevel.Information, Message = "Employee {EmployeeId} registered")]
    public static partial void EmployeeRegistered(this ILogger logger, Guid employeeId);
}
