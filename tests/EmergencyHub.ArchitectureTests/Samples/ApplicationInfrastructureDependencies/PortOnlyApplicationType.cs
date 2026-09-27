using EmergencyHub.BuildingBlocks.Application.Persistence;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationInfrastructureDependencies;

/// <summary>규칙을 지킨 예: Application 포트(IUnitOfWork)만 쓴다.</summary>
/// <param name="unitOfWork">Unit of Work.</param>
public sealed class PortOnlyApplicationType(IUnitOfWork unitOfWork)
{
    /// <summary>Unit of Work.</summary>
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;
}
