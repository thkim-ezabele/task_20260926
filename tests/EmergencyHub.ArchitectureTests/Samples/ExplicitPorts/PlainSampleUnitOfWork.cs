using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.ExplicitPorts;

/// <summary>규칙을 지킨 예: 마커 없는 명시 등록 포트 구현.</summary>
public sealed class PlainSampleUnitOfWork : IUnitOfWork
{
    /// <inheritdoc />
    public Task<Result> CommitAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}
