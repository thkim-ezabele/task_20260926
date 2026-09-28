using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Entities;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Events;
using EmergencyHub.BuildingBlocks.Domain.Identifiers;
using EmergencyHub.BuildingBlocks.Domain.Repositories;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 형식 형태에 관한 컨벤션 규칙(sealed · record · 마커 · 기반 클래스 · 강타입 ID · enum · 가시성).
/// 원본: coding-conventions, testing-strategy "아키텍처 테스트", PRD-001 FR-09, BL-029(BL-054 병합) · BL-072.
/// </summary>
public static class ConventionRules
{
    // coding-conventions "C# 언어 기능" 클래스는 기본 sealed 행의 BuildingBlocks.Domain 예외(abstract 기반은 대상에서 이미 빠짐).
    private static readonly Type[] OpenByDesign = [typeof(Error), typeof(Result)];

    // 파생을 허용하는 형식과 그 파생(BuildingBlocks.Domain 안에서만): ValidationError · ConflictError : Error, Result<T> : Result.
    private static readonly Type[] ErrorResultFamily = [typeof(Error), typeof(ValidationError), typeof(ConflictError), typeof(Result), typeof(Result<>)];

    private static readonly string[] ModelNameSuffixes = ["Command", "Query", "Request", "Response", "Dto", "Event"];

    private static readonly Type[] OpenGenericImplementationPorts = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    private static readonly Type[] ImplementationPorts =
    [
        typeof(IValidator),
        typeof(IRepository),
        typeof(IReadRepository),
        typeof(ISender),
        typeof(IUnitOfWork),
        typeof(IExceptionClassifier),
        typeof(IIdGenerator),
        typeof(IPreCommitHook),
        typeof(IExceptionHandler),
        typeof(ISchemaFilter),
    ];

    private static readonly Type[] ExplicitlyRegisteredPorts = [typeof(IUnitOfWork), typeof(IExceptionClassifier), typeof(IIdGenerator), typeof(IPreCommitHook)];

    private static readonly Type[] RegistrationMarkers = [typeof(IService), typeof(IRepository), typeof(IReadRepository)];

    /// <summary>추상이 아닌 클래스는 sealed다(예외: <see cref="Error"/> · <see cref="Result"/>).</summary>
    public static ArchitectureRule ClassesAreSealed { get; } = new(
        "클래스는 기본 sealed",
        "coding-conventions \"C# 언어 기능\" 클래스는 기본 sealed 행(예외: Error · Result), PRD-001 FR-09 컨벤션 규칙(sealed)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type => TypeInspection.IsConcreteClass(type) && !OpenByDesign.Contains(type))),
        conditions => conditions.BeSealed());

    /// <summary>Command · Query · Request · Response · Dto · 이벤트는 record다.</summary>
    public static ArchitectureRule RequestAndResponseModelsAreRecords { get; } = new(
        "Command · Query · Request · Response · Dto · 이벤트는 record",
        "coding-conventions \"C# 언어 기능\" record 행, testing-strategy 아키텍처 테스트(record 이름 규칙), PRD-001 FR-09 컨벤션 규칙(Command / Query / Response는 record)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(IsRequestOrResponseModel)),
        conditions => conditions.MeetCustomRule(new TypeRule(TypeInspection.IsRecord)),
        TargetsOnlyInServices: true);

    /// <summary>Repository 인터페이스는 마커를 상속한다(<c>*ReadRepository</c> → <see cref="IReadRepository"/>, 그 밖 <c>*Repository</c> → <see cref="IRepository"/>).</summary>
    public static ArchitectureRule RepositoryInterfacesInheritMarkers { get; } = new(
        "Repository 인터페이스는 IRepository / IReadRepository 상속",
        "coding-conventions \"의존성 주입 (DI) 규칙\" 마커 표, testing-strategy 아키텍처 테스트, PRD-001 FR-09 컨벤션 규칙(Repository 인터페이스의 마커 상속)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            type.IsInterface && type.Name.EndsWith("Repository", StringComparison.Ordinal) && !RegistrationMarkers.Contains(type))),
        conditions => conditions.MeetCustomRule(new TypeRule(InheritsMatchingRepositoryMarker)),
        TargetsOnlyInServices: true);

    /// <summary>Repository 구현은 쓰기면 <see cref="RepositoryBase{TContext}"/>, 읽기면 <see cref="ReadRepositoryBase{TContext}"/>에서 파생한다.</summary>
    public static ArchitectureRule RepositoryImplementationsDeriveFromBases { get; } = new(
        "Repository 구현은 RepositoryBase / ReadRepositoryBase 파생",
        "coding-conventions \"의존성 주입 (DI) 규칙\" 마커 표, testing-strategy 아키텍처 테스트, S02-T04 규칙 후보",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            TypeInspection.IsConcreteClass(type) && (typeof(IRepository).IsAssignableFrom(type) || typeof(IReadRepository).IsAssignableFrom(type)))),
        conditions => conditions.MeetCustomRule(new TypeRule(DerivesFromMatchingRepositoryBase)),
        TargetsOnlyInServices: true);

    /// <summary><see cref="IStronglyTypedId{TSelf}"/> 구현은 형식 인자가 자기 자신이다(다르면 값 변환기 등록에서 조용히 빠짐, BL-072).</summary>
    public static ArchitectureRule StronglyTypedIdsReferenceThemselves { get; } = new(
        "IStronglyTypedId<TSelf>의 TSelf는 구현 형식 자신",
        "BL-072, coding-conventions \"영속성 기반 형식\" IStronglyTypedId<TSelf> 행",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            !type.IsInterface && TypeInspection.ImplementsOpenGeneric(type, typeof(IStronglyTypedId<>)))),
        conditions => conditions.MeetCustomRule(new TypeRule(type => type.GetInterfaces()
            .Where(implemented => implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>))
            .All(implemented => implemented.GetGenericArguments()[0] == type))),
        TargetsOnlyInServices: true);

    /// <summary>Entity 키 형식은 강타입 ID(<see cref="IStronglyTypedId{TSelf}"/>)다.</summary>
    public static ArchitectureRule EntityIdsAreStronglyTyped { get; } = new(
        "Entity<TId>의 TId는 IStronglyTypedId<TId> 구현",
        "coding-conventions \"C# 언어 기능\" record struct 행 · \"영속성 기반 형식\" IStronglyTypedId<TSelf> 행, S02-T04 규칙 후보",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            TypeInspection.IsConcreteClass(type) && TypeInspection.FindOpenGenericBase(type, typeof(Entity<>)) is not null)),
        conditions => conditions.MeetCustomRule(new TypeRule(type =>
        {
            var id = TypeInspection.FindOpenGenericBase(type, typeof(Entity<>))!.GetGenericArguments()[0];

            // MakeGenericType은 제약(struct, IStronglyTypedId<TSelf>)을 어기는 키(Guid 등)에서 예외를 던지므로 구현 인터페이스로 판별한다.
            return id.GetInterfaces().Any(implemented =>
                implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>) && implemented.GetGenericArguments()[0] == id);
        })),
        TargetsOnlyInServices: true);

    /// <summary>
    /// enum 기반 형식: 일반 코드는 <see cref="short"/>, <see cref="FlagsAttribute"/>는 <see cref="int"/> 또는 <see cref="long"/>.
    /// </summary>
    /// <remarks>
    /// 컴파일된 메타데이터는 <c>enum X</c>와 <c>enum X : int</c>를 구별하지 못한다(리플렉션 · Cecil 모두). 그래서 "명시" 자체가 아니라
    /// 결과 기반 형식을 강제한다: 일반 enum에서 명시를 빠뜨리면 <see cref="int"/>가 되어 이 규칙이 잡는다. [Flags] enum의 <c>: int</c> 생략만
    /// 규칙 밖(사람 리뷰)으로 남는다(S02-T04 reviewer 인계).
    /// </remarks>
    public static ArchitectureRule CodeEnumsUseConventionalUnderlyingTypes { get; } = new(
        "enum 기반 형식: 일반 short, [Flags] int / long",
        "coding-conventions \"코드값 (enum) 규칙\"(기반 형식을 명시한다. 일반 코드는 short, 비트 플래그는 int 또는 long), ADR-0008",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type => type.IsEnum)),
        conditions => conditions.MeetCustomRule(new TypeRule(HasConventionalUnderlyingType)));

    /// <summary>Handler · Validator · Repository 구현과 BuildingBlocks 포트 구현은 internal sealed다.</summary>
    public static ArchitectureRule ImplementationsAreInternalSealed { get; } = new(
        "Handler · Validator · Repository · 포트 구현은 internal sealed",
        "coding-conventions CQRS 예시(internal sealed Handler) · Validator 규칙 · Repository 예시, ADR-0015(구현 형식 internal), S02-T01 · T02 · T07 · T06 인계",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(IsImplementationOfPort)),
        conditions => conditions.MeetCustomRule(new TypeRule(type => !type.IsPublic && !type.IsNestedPublic && type.IsSealed)));

    /// <summary>Validator(FluentValidation <see cref="IValidator"/> 구현)는 <see cref="RequestValidator{TRequest}"/>에서 파생한다.</summary>
    public static ArchitectureRule ValidatorsDeriveFromRequestValidator { get; } = new(
        "Validator는 RequestValidator<T> 파생",
        "coding-conventions CQRS 규칙(Validator는 internal sealed class : RequestValidator<요청>), ADR-0018, S02-T02 인계",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type => TypeInspection.IsConcreteClass(type) && typeof(IValidator).IsAssignableFrom(type))),
        conditions => conditions.MeetCustomRule(new TypeRule(type => TypeInspection.FindOpenGenericBase(type, typeof(RequestValidator<>)) is not null)),
        TargetsOnlyInServices: true);

    /// <summary>명시 등록 포트(<see cref="IUnitOfWork"/> · <see cref="IExceptionClassifier"/> · <see cref="IIdGenerator"/> · <see cref="IPreCommitHook"/>) 구현은 자동 등록 마커를 구현하지 않는다.</summary>
    public static ArchitectureRule ExplicitlyRegisteredPortsDoNotImplementMarkers { get; } = new(
        "명시 등록 포트 구현은 IService · IRepository · IReadRepository 미구현",
        "coding-conventions \"BuildingBlocks 공통 등록 진입점\" 표(IIdGenerator · AddUnitOfWork · 분류기 명시 등록), S02-T01 · T03 · T07 인계(이중 등록 방지)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            TypeInspection.IsConcreteClass(type) && ExplicitlyRegisteredPorts.Any(port => port.IsAssignableFrom(type)))),
        conditions => conditions.MeetCustomRule(new TypeRule(type => !RegistrationMarkers.Any(marker => marker.IsAssignableFrom(type)))));

    /// <summary>
    /// <see cref="Error"/> · <see cref="Result"/>는 BuildingBlocks.Domain의 정해진 파생(<see cref="ValidationError"/> · <see cref="ConflictError"/> · <see cref="Result{T}"/>)
    /// 밖에서 파생하지 않는다.
    /// </summary>
    public static ArchitectureRule ErrorAndResultAreNotDerived { get; } = new(
        "Error / Result 파생 금지(ValidationError · ConflictError · Result<T> 제외)",
        "BL-029(BL-054 병합), TD-016(Error는 복사 생성자로 어셈블리 밖 파생 가능), coding-conventions \"C# 언어 기능\" sealed 예외, ADR-0028(상세 Conflict 오류)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type => type.IsClass && !ErrorResultFamily.Contains(type))),
        conditions => conditions.NotInherit(typeof(Error)).And().NotInherit(typeof(Result)));

    /// <summary>Entity · Aggregate 파생 클래스는 sealed다(예외: BuildingBlocks.Domain의 <see cref="AggregateRoot{TId}"/>).</summary>
    public static ArchitectureRule EntityDerivedTypesAreSealed { get; } = new(
        "Entity / AggregateRoot 파생은 sealed(abstract 중간 기반 금지)",
        "BL-029(BL-054 병합), coding-conventions \"C# 언어 기능\" sealed 예외(Entity<TId> · AggregateRoot<TId>만 abstract)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            type.IsClass && !IsOpenGeneric(type, typeof(AggregateRoot<>)) && TypeInspection.FindOpenGenericBase(type, typeof(Entity<>)) is not null)),
        conditions => conditions.BeSealed(),
        TargetsOnlyInServices: true);

    private static bool IsRequestOrResponseModel(Type type)
    {
        if (type.IsInterface || type.IsEnum)
        {
            return false;
        }

        return TypeInspection.ImplementsOpenGeneric(type, typeof(ICommand<>))
            || TypeInspection.ImplementsOpenGeneric(type, typeof(IQuery<>))
            || typeof(IDomainEvent).IsAssignableFrom(type)
            || ModelNameSuffixes.Any(suffix => TypeInspection.SimpleName(type).EndsWith(suffix, StringComparison.Ordinal));
    }

    private static bool InheritsMatchingRepositoryMarker(Type type) =>
        type.Name.EndsWith("ReadRepository", StringComparison.Ordinal)
            ? typeof(IReadRepository).IsAssignableFrom(type) && !typeof(IRepository).IsAssignableFrom(type)
            : typeof(IRepository).IsAssignableFrom(type) && !typeof(IReadRepository).IsAssignableFrom(type);

    private static bool DerivesFromMatchingRepositoryBase(Type type)
    {
        var isWrite = typeof(IRepository).IsAssignableFrom(type);
        var isRead = typeof(IReadRepository).IsAssignableFrom(type);
        var writeBase = TypeInspection.FindOpenGenericBase(type, typeof(RepositoryBase<>)) is not null;
        var readBase = TypeInspection.FindOpenGenericBase(type, typeof(ReadRepositoryBase<>)) is not null;

        return isWrite != isRead && (isWrite ? writeBase : readBase);
    }

    private static bool HasConventionalUnderlyingType(Type type)
    {
        var underlying = Enum.GetUnderlyingType(type);

        return type.IsDefined(typeof(FlagsAttribute), inherit: false)
            ? underlying == typeof(int) || underlying == typeof(long)
            : underlying == typeof(short);
    }

    private static bool IsImplementationOfPort(Type type) =>
        TypeInspection.IsConcreteClass(type)
        && (OpenGenericImplementationPorts.Any(port => TypeInspection.ImplementsOpenGeneric(type, port))
            || ImplementationPorts.Any(port => port.IsAssignableFrom(type)));

    private static bool IsOpenGeneric(Type type, Type openGeneric) => type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric;
}
