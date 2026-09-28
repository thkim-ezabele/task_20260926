using Scrutor;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;

/// <summary>위반 예: Scrutor 형식을 쓴다.</summary>
public sealed class ScrutorApplicationType
{
    /// <summary>등록 전략.</summary>
    public RegistrationStrategy? Strategy { get; init; }
}
