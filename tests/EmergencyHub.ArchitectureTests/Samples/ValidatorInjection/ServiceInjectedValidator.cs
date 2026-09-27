using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.ArchitectureTests.Samples.ValidatorInjection;

/// <summary>위반 예: 서비스를 주입받는다.</summary>
/// <param name="service">주입 대상.</param>
public sealed class ServiceInjectedValidator(ISampleService service) : RequestValidator<SampleCommand>
{
    /// <summary>주입 대상.</summary>
    public ISampleService Dependency { get; } = service;
}
