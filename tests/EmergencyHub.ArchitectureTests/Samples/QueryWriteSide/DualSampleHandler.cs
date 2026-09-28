using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.QueryWriteSide;

/// <summary>위반 예: 한 형식이 Query Handler와 Command Handler를 함께 구현(트랜잭션 데코레이터가 Query를 감쌀 수 있음).</summary>
public sealed class DualSampleHandler : IQueryHandler<SampleQuery, Unit>, ICommandHandler<SampleCommand>
{
    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
}
