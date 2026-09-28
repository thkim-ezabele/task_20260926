using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.ArchitectureTests.Samples.DomainDependencies;

/// <summary>위반 예: 바깥 레이어(Application)의 형식을 쓴다.</summary>
public sealed class SenderUsingDomainType
{
    /// <summary>디스패처.</summary>
    public ISender? Sender { get; init; }
}
