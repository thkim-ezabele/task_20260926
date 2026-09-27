using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.QueryWriteSide;

/// <summary>위반 예: Query Handler가 Write Repository를 받는다(Query는 Read Repository로만 조회).</summary>
/// <param name="repository">Write Repository.</param>
public sealed class WriteRepositorySampleQueryHandler(ISampleWriteRepository repository) : IQueryHandler<SampleQuery, Unit>
{
    /// <summary>Write Repository.</summary>
    public ISampleWriteRepository Repository { get; } = repository;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
