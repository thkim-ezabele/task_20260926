namespace EmergencyHub.BuildingBlocks.Application.Identifiers;

/// <summary>
/// 새 식별자(UUID v7)를 만듭니다. Handler가 ID를 만들어 Aggregate 팩토리에 넘깁니다(ADR-0013).
/// </summary>
/// <remarks>
/// 구현은 BuildingBlocks.Infrastructure에 두고 UUIDNext <c>Uuid.NewDatabaseFriendly(Database.PostgreSql)</c>를 씁니다.
/// 단위 테스트에서는 대역을 씁니다.
/// DI 자동 등록 마커(<c>IService</c>)를 상속하지 않고, Infrastructure의 <c>AddBuildingBlocksInfrastructure()</c>가 Scoped로 명시 등록합니다
/// (ADR-0017 "공통 인프라 등록", 수명은 ADR-0013. 구현이 서비스 어셈블리 밖에 있어 어셈블리 검색으로는 찾을 수 없음).
/// </remarks>
public interface IIdGenerator
{
    /// <summary>새 식별자를 만듭니다.</summary>
    /// <returns>시간 순으로 정렬되는 UUID v7. 빈 값(<see cref="Guid.Empty"/>)이 아닙니다.</returns>
    Guid NewId();
}
