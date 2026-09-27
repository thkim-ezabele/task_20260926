using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.ArchitectureTests.Samples.ErrorDerivation;

/// <summary>위반 예: 복사 생성자로 어셈블리 밖에서 Error를 파생(TD-016).</summary>
public sealed record CustomError : Error
{
    /// <summary>표본 생성자.</summary>
    /// <param name="original">원본 오류.</param>
    public CustomError(Error original)
        : base(original)
    {
    }
}
