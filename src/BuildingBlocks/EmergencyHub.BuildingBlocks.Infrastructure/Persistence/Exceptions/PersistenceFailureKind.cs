namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// 영속성 예외 변환 결과의 종류입니다. 변환 로그(201 ~ 203)를 고르는 데만 씁니다(database.md "영속성 예외 변환").
/// </summary>
internal enum PersistenceFailureKind : short
{
    /// <summary>예약 값(쓰지 않음).</summary>
    None = 0,

    /// <summary>규칙 1: 낙관적 동시성 충돌(3001, 로그 203).</summary>
    ConcurrencyConflict = 1,

    /// <summary>규칙 2: 레지스트리에 있는 유니크 인덱스 위반(서비스 오류, 로그 201).</summary>
    MappedUniqueViolation = 2,

    /// <summary>규칙 3: 레지스트리에 없는 유니크 제약 위반(3003, 로그 202).</summary>
    UnmappedUniqueViolation = 3,
}
