using EmergencyHub.ArchitectureTests.Samples.ServiceIsolationOrderingReports;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ServiceIsolation;

/// <summary>
/// 규칙을 지킨 예: BuildingBlocks 형식과, 금지 서비스와 이름 접두사만 같은(점 경계가 다른) 형식을 쓴다.
/// </summary>
public sealed class BuildingBlocksOnlyServiceType
{
    /// <summary>오류.</summary>
    public Error? Error { get; init; }

    /// <summary>이름 접두사만 같은 다른 네임스페이스의 형식.</summary>
    public ReportSummary? Summary { get; init; }
}
