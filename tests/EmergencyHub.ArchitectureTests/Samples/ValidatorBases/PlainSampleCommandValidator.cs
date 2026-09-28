using EmergencyHub.ArchitectureTests.Samples.Fixtures;
using FluentValidation;

namespace EmergencyHub.ArchitectureTests.Samples.ValidatorBases;

/// <summary>위반 예: 공통 기반 없이 AbstractValidator에서 바로 파생(첫 실패 중단 설정이 빠짐).</summary>
public sealed class PlainSampleCommandValidator : AbstractValidator<SampleCommand>;
