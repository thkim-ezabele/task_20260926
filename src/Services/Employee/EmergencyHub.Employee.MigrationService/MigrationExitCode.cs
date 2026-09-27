namespace EmergencyHub.Employee.MigrationService;

/// <summary>
/// MigrationService 프로세스 종료 코드입니다. AppHost <c>WaitForCompletion</c>은 0일 때만 Api를 시작합니다(ADR-0012).
/// 일반 코드 enum 규칙대로 기반 형식은 <see langword="short"/>이고, 프로세스 종료 코드 · 로그에는 <see langword="int"/>로 넘깁니다(ADR-0008).
/// </summary>
internal enum MigrationExitCode : short
{
    /// <summary>모든 마이그레이션을 적용했습니다(적용할 것이 없던 경우 포함).</summary>
    Succeeded = 0,

    /// <summary>예외로 실패했습니다(연결 · 인증 · SQL 오류, 재시도 한도 초과 등).</summary>
    Failed = 1,

    /// <summary>적용 도중 취소되었습니다(호스트 종료 신호). 성공이 아닙니다.</summary>
    Canceled = 2,
}
