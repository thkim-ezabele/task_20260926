namespace EmergencyHub.ArchitectureTests.Assemblies;

/// <summary>제품 프로젝트의 소유(공유 BuildingBlocks인가, 서비스인가). 규칙 범위를 나눌 때 쓴다(ADR-0024 BuildingBlocks 고유 행 · 서비스 행).</summary>
public enum AssemblyOwnership : short
{
    /// <summary>BuildingBlocks도 서비스도 아니다(ServiceDefaults · AppHost · 외부 패키지). 검사 대상 목록에 쓰지 않는다.</summary>
    Unknown = 0,

    /// <summary>공유 빌딩 블록(<c>EmergencyHub.BuildingBlocks.*</c>).</summary>
    BuildingBlocks = 1,

    /// <summary>서비스 프로젝트(<c>EmergencyHub.&lt;Service&gt;.*</c>).</summary>
    Service = 2,
}
