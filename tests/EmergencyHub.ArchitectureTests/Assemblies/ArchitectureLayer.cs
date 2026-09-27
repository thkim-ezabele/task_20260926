namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>검사 대상 어셈블리의 레이어(clean-architecture "레이어 구성").</summary>
public enum ArchitectureLayer : short
{
    /// <summary>예약 값. 목록에 쓰지 않는다.</summary>
    Unknown = 0,

    /// <summary>Domain 레이어(BuildingBlocks.Domain, <c>&lt;Service&gt;.Domain</c>).</summary>
    Domain = 1,

    /// <summary>Application 레이어.</summary>
    Application = 2,

    /// <summary>Infrastructure 레이어.</summary>
    Infrastructure = 3,

    /// <summary>Api 레이어(BuildingBlocks.Api, <c>&lt;Service&gt;.Api</c>).</summary>
    Api = 4,

    /// <summary>마이그레이션 적용 워커(<c>&lt;Service&gt;.MigrationService</c>, ADR-0012). BuildingBlocks에는 없다.</summary>
    MigrationService = 5,
}
