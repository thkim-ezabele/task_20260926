namespace EmergencyHub.ArchitectureTests.Samples.RecordModels;

/// <summary>위반 예: record가 아닌 struct DTO.</summary>
/// <param name="name">이름.</param>
public readonly struct StructSummaryDto(string name)
{
    /// <summary>이름.</summary>
    public string Name { get; } = name;
}
