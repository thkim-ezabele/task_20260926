using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

// 마커(IService 등)를 상속하지 않는 포트의 구현 예시. 검색 대상 어셈블리에 있어도 AddConventionalServices가 등록하지 않아야 한다
// (IUnitOfWork는 S02-T07 등록 확장, IExceptionClassifier는 여러 구현, IIdGenerator는 AddBuildingBlocksInfrastructure가 명시 등록).

/// <summary>검색 대상에 있지만 자동 등록되면 안 되는 <see cref="IUnitOfWork"/> 구현.</summary>
public sealed class SampleUnitOfWork : IUnitOfWork
{
    /// <inheritdoc />
    public Task<Result> CommitAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success());
}

/// <summary>검색 대상에 있지만 자동 등록되면 안 되는 <see cref="IExceptionClassifier"/> 구현.</summary>
public sealed class SampleExceptionClassifier : IExceptionClassifier
{
    /// <inheritdoc />
    public Error? Classify(Exception exception) => null;
}

/// <summary>검색 대상에 있지만 자동 등록되면 안 되는 <see cref="IIdGenerator"/> 구현.</summary>
public sealed class SampleIdGenerator : IIdGenerator
{
    /// <inheritdoc />
    public Guid NewId() => Guid.NewGuid();
}
