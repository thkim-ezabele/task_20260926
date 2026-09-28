using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Domain.Entities;

namespace EmergencyHub.ArchitectureTests.Samples.EntitySealing;

/// <summary>위반 예: 서비스 코드의 abstract 중간 Aggregate 기반.</summary>
public abstract class AbstractSampleAggregate : AggregateRoot<SampleId>
{
    /// <summary>표본 생성자.</summary>
    /// <param name="id">ID.</param>
    protected AbstractSampleAggregate(SampleId id)
        : base(id)
    {
    }
}
