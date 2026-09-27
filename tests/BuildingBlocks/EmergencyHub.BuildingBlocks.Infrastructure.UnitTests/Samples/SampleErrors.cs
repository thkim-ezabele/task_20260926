using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>테스트용 오류 정의(서비스 자리 2 가정).</summary>
public static class SampleErrors
{
    /// <summary>값이 음수(검증 실패).</summary>
    public static readonly Error ValueNegative = Error.Validation(21001, "값은 0 이상이어야 합니다.");

    /// <summary>Handler가 돌려주는 규칙 실패(충돌).</summary>
    public static readonly Error ValueConflict = Error.Conflict(23001, "이미 처리된 값입니다.");
}
