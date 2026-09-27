using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.ArchitectureTests.Samples.HandlerSender;

/// <summary>규칙을 지킨 예: ISender 없이 처리하는 Handler.</summary>
/// <param name="timeProvider">시각 공급자.</param>
public sealed class PlainSampleCommandHandler(TimeProvider timeProvider) : ICommandHandler<SampleCommand>
{
    /// <summary>시각 공급자.</summary>
    public TimeProvider TimeProvider { get; } = timeProvider;

    /// <inheritdoc />
    public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
}
