using System.Diagnostics.CodeAnalysis;
using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using EmergencyHub.BuildingBlocks.Application.Validation;

namespace EmergencyHub.ArchitectureTests.Samples.ValidatorBases;

/// <summary>규칙을 지킨 예: RequestValidator 파생.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = SampleSuppressions.MetadataOnly)]
internal sealed class InternalSampleCommandValidator : RequestValidator<SampleCommand>;
