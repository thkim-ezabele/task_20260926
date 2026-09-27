namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 공통 규칙이 owned가 아닌 엔티티 형식마다 추가하는 shadow property 이름입니다. Domain 엔티티에는 이 속성들이 없습니다.
/// </summary>
/// <remarks>
/// Read Repository 프로젝션에서는 <c>EF.Property&lt;DateTimeOffset&gt;(e, ShadowPropertyNames.CreatedAt)</c>처럼 이 상수를 씁니다(문자열 리터럴 금지, database.md "감사 컬럼").
/// </remarks>
public static class ShadowPropertyNames
{
    /// <summary>생성 시각(<c>created_at</c>, <c>timestamptz</c>, NOT NULL). 감사 인터셉터가 Added일 때 채웁니다.</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>수정 시각(<c>updated_at</c>, <c>timestamptz</c>, NOT NULL). Added · Modified · owned 변경 시 감사 인터셉터가 채웁니다.</summary>
    public const string UpdatedAt = "UpdatedAt";

    /// <summary>낙관적 동시성 토큰(<c>uint</c>, PostgreSQL 시스템 컬럼 <c>xmin</c> · <c>xid</c>). 마이그레이션이 만들지 않습니다.</summary>
    public const string Version = "Version";
}
