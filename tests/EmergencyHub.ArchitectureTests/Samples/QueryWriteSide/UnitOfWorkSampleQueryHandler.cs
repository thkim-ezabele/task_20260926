using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.QueryWriteSide;

/// <summary>위반 예: Query Handler가 IUnitOfWork를 받는다(Query에 트랜잭션).</summary>
/// <param name="unitOfWork">Unit of Work.</param>
public sealed class UnitOfWorkSampleQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<SampleQuery, Unit>
{
    /// <summary>Unit of Work.</summary>
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
