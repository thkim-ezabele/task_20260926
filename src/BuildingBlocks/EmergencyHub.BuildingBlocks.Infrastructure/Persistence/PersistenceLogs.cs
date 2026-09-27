using Microsoft.Extensions.Logging;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork 영속성 예외 변환 로그 정의입니다. 이벤트 ID는 공통 범위 중 영속성 하위 범위 201 ~ 299를 씁니다
/// (원본: wiki/05-api/error-codes.md "영속성 로그 이벤트", BL-028).
/// </summary>
/// <remarks>
/// 속성은 제약 이름 · SqlState · 엔티티 형식 짧은 이름 · 에러 코드뿐이고, <c>Detail</c> · <c>MessageText</c> · 값 · 키와 예외 객체는 남기지 않습니다.
/// 실패 결과는 로깅 데코레이터가 102(Information)로 이미 남기므로 여기서는 Information을 쓰지 않습니다:
/// 매핑된 23505 · 동시성 충돌은 Debug, 매핑 없는 23505는 매핑 누락 또는 TD-010 신호라 Warning입니다. 재시도 로그는 EF Core 실행 전략이 남깁니다.
/// </remarks>
internal static partial class PersistenceLogs
{
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes}, returned error {ErrorCode}")]
    public static partial void UniqueConstraintViolationMapped(this ILogger logger, string? constraintName, string? sqlState, string entityTypes, int errorCode);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Warning,
        Message = "Unique constraint {ConstraintName} violated with SqlState {SqlState} on {EntityTypes} has no error mapping, returned error {ErrorCode}")]
    public static partial void UniqueConstraintViolationUnmapped(this ILogger logger, string? constraintName, string? sqlState, string entityTypes, int errorCode);

    [LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "Concurrency conflict on {EntityTypes}, returned error {ErrorCode}")]
    public static partial void ConcurrencyConflictDetected(this ILogger logger, string entityTypes, int errorCode);
}
