using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Repositories;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

// 이 테스트 어셈블리는 AddConventionalServices의 검색 대상이다. 여기 있는 구현은 규칙을 지키는 예시만 둔다.
// 규칙을 어기는 구현(중복, 인터페이스 둘, 마커 직접 구현)은 DynamicSampleAssembly로 별도 어셈블리에 만든다.

/// <summary>Write Repository 인터페이스 예시(<see cref="IRepository"/> 상속).</summary>
public interface ISampleRepository : IRepository;

/// <summary><see cref="ISampleRepository"/> 구현.</summary>
public sealed class SampleRepository : ISampleRepository;

/// <summary>Read Repository 인터페이스 예시(<see cref="IReadRepository"/> 상속).</summary>
public interface ISampleReadRepository : IReadRepository;

/// <summary><see cref="ISampleReadRepository"/> 구현.</summary>
public sealed class SampleReadRepository : ISampleReadRepository;

/// <summary>서비스 인터페이스 예시(<see cref="IService"/> 상속).</summary>
public interface ISampleService : IService;

/// <summary><see cref="ISampleService"/> 구현. 다른 마커 구현을 주입받아 의존 해석까지 확인한다.</summary>
public sealed class SampleService(ISampleRepository repository) : ISampleService
{
    /// <summary>주입받은 Repository.</summary>
    public ISampleRepository Repository { get; } = repository;
}

/// <summary>internal 구현이 검색되는지 확인하기 위한 서비스 인터페이스.</summary>
public interface IInternalSampleService : IService;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 internal 구현도 검색해 DI가 만든다(테스트 대상 동작).")]
internal sealed class InternalSampleService : IInternalSampleService;

/// <summary>구현이 추상 클래스뿐인 서비스 인터페이스. 추상 클래스는 등록하지 않는다.</summary>
public interface IAbstractOnlySampleService : IService;

/// <summary><see cref="IAbstractOnlySampleService"/>의 추상 구현(등록 대상 아님).</summary>
public abstract class AbstractOnlySampleService : IAbstractOnlySampleService;
