using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>서비스 에러 코드 예시(Employee 자리 2). 실제 Employee 코드 할당은 S03입니다.</summary>
internal static class SampleErrors
{
    public static readonly Error InvalidEmail = Error.Validation(21001, "이메일 형식이 아닙니다.");

    public static readonly Error NameRequired = Error.Validation(21002, "이름은 필수입니다.");

    public static readonly Error EmployeeNotFound = Error.NotFound(22001, "직원을 찾을 수 없습니다.");

    public static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");

    public static readonly Error InvalidTransition = Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다.");
}
