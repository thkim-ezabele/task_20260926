using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// 영속성 예외를 바꾼 결과입니다. 로그에 남길 수 있는 값(제약 이름 · SqlState · 엔티티 형식 이름)만 담고 Detail · MessageText · 값은 담지 않습니다.
/// </summary>
/// <param name="Error">돌려줄 오류(고정 문구).</param>
/// <param name="Kind">변환 종류(로그 이벤트 선택).</param>
/// <param name="ConstraintName">제약 이름. 동시성 충돌이면 <see langword="null"/>.</param>
/// <param name="SqlState">SqlState. 동시성 충돌이면 <see langword="null"/>.</param>
/// <param name="EntityTypeNames">실패한 엔트리의 엔티티 형식 짧은 이름(중복 제거).</param>
internal sealed record PersistenceExceptionTranslation(
    Error Error,
    PersistenceFailureKind Kind,
    string? ConstraintName,
    string? SqlState,
    IReadOnlyList<string> EntityTypeNames);
