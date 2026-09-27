using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.ArchitectureTests.Samples.ValidatorInjection;

/// <summary>규칙을 지킨 예: Repository · 서비스가 아닌 의존만 받는다.</summary>
/// <param name="timeProvider">주입 대상.</param>
public sealed class TimeAwareSampleValidator(TimeProvider timeProvider) : RequestValidator<SampleCommand>
{
    /// <summary>주입 대상.</summary>
    public TimeProvider Dependency { get; } = timeProvider;
}
