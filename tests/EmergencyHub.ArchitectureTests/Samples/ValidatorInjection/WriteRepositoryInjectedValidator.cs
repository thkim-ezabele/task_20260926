using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.ArchitectureTests.Samples.ValidatorInjection;

/// <summary>위반 예: Write Repository를 주입받는다(Validator DB 접근).</summary>
/// <param name="repository">주입 대상.</param>
public sealed class WriteRepositoryInjectedValidator(ISampleWriteRepository repository) : RequestValidator<SampleCommand>
{
    /// <summary>주입 대상.</summary>
    public ISampleWriteRepository Dependency { get; } = repository;
}
