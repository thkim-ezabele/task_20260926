using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>검색 대상에 있지만 자동 등록되면 안 되는 <see cref="IExceptionClassifier"/> 구현.</summary>
/// <remarks>
/// 마커(<c>IService</c> 등)를 상속하지 않는 포트의 구현 예시입니다. 검색 대상 어셈블리에 있어도 AddConventionalServices가 등록하지 않아야 합니다
/// (IUnitOfWork는 S02-T07 등록 확장, IExceptionClassifier는 여러 구현, IIdGenerator는 AddBuildingBlocksInfrastructure가 명시 등록).
/// </remarks>
public sealed class SampleExceptionClassifier : IExceptionClassifier
{
    /// <inheritdoc />
    public Error? Classify(Exception exception) => null;
}
