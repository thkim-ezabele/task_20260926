using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;

/// <summary>위반 예: 한 인자 Handler 인터페이스를 구현한 public Handler(두 인자 형태로도 선택됨).</summary>
public sealed class PublicSampleCommandHandler : ICommandHandler<SampleCommand>
{
    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
}
