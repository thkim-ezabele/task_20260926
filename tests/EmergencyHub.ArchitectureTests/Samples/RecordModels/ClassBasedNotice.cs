using EmergencyHub.BuildingBlocks.Domain.Events;

namespace EmergencyHub.ArchitectureTests.Samples.RecordModels;

/// <summary>위반 예: class로 만든 도메인 이벤트(이름이 아니라 IDomainEvent 구현으로 선택).</summary>
public sealed class ClassBasedNotice : IDomainEvent
{
    /// <summary>발생 시각.</summary>
    public DateTimeOffset OccurredAt { get; init; }
}
