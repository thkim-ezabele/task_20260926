using FluentValidation;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationFrameworkDependencies;

/// <summary>규칙을 지킨 예: 허용된 FluentValidation과 BCL만 쓴다.</summary>
public sealed class FluentValidationApplicationType
{
    /// <summary>검증기.</summary>
    public IValidator? Validator { get; init; }

    /// <summary>서비스 공급자.</summary>
    public IServiceProvider? Services { get; init; }
}
