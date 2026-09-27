using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.QueryWriteSide;

/// <summary>규칙을 지킨 예: Query Handler가 Read Repository만 받는다.</summary>
/// <param name="repository">Read Repository.</param>
public sealed class ReadRepositorySampleQueryHandler(ISampleReadRepository repository) : IQueryHandler<SampleQuery, Unit>
{
    /// <summary>Read Repository.</summary>
    public ISampleReadRepository Repository { get; } = repository;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
