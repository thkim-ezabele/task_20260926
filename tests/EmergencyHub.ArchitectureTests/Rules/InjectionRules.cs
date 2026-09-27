using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Repositories;
using FluentValidation;

namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 무엇을 주입받는지에 관한 규칙(CQRS · 파이프라인). 원본: coding-conventions "CQRS 규칙", ADR-0007 · 0014 · 0015 · 0018, BL-029.
/// </summary>
public static class InjectionRules
{
    /// <summary>Handler(<c>ICommandHandler&lt;,&gt;</c> · <c>IQueryHandler&lt;,&gt;</c> 구현)는 <see cref="ISender"/>에 의존하지 않는다(중첩 Send → 커밋 두 번).</summary>
    public static ArchitectureRule HandlersDoNotDependOnSender { get; } = new(
        "Handler ↛ ISender",
        "coding-conventions \"CQRS 규칙\"(Handler는 ISender를 주입받지 않는다), ADR-0015(중첩 Send 금지), BL-029",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(IsHandler)),
        conditions => conditions.NotHaveDependencyOn(typeof(ISender).FullName));

    /// <summary>Validator는 Repository · 서비스를 주입받지 않는다(Validator DB 접근 금지).</summary>
    public static ArchitectureRule ValidatorsDoNotInjectRepositoriesOrServices { get; } = new(
        "Validator ↛ IRepository · IReadRepository · IService 주입",
        "ADR-0018(Validator DB 접근 금지), BL-029",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type => TypeInspection.IsConcreteClass(type) && typeof(IValidator).IsAssignableFrom(type))),
        conditions => conditions.MeetCustomRule(new TypeRule(type =>
            !TypeInspection.InjectsAnyOf(type, typeof(IRepository), typeof(IReadRepository), typeof(IService)))),
        TargetsOnlyInServices: true);

    /// <summary>Query 경로(<c>IQueryHandler&lt;,&gt;</c> 구현)는 쓰기 측(<see cref="IUnitOfWork"/> · Write Repository · Command Handler)을 모른다.</summary>
    public static ArchitectureRule QueryHandlersDoNotUseWriteSide { get; } = new(
        "Query Handler ↛ IUnitOfWork · Write Repository · ICommandHandler",
        "coding-conventions \"CQRS 규칙\"(Query는 Read Repository로만 조회, 트랜잭션은 Command에만), ADR-0007 · 0009 · 0014, S02-T02 인계(트랜잭션 데코레이터는 IQueryHandler 미구현)",
        RuleScope.Product(),
        scope => scope.And().MeetCustomRule(new TypeRule(type =>
            TypeInspection.IsConcreteClass(type) && TypeInspection.ImplementsOpenGeneric(type, typeof(IQueryHandler<,>)))),
        conditions => conditions.MeetCustomRule(new TypeRule(type =>
            !TypeInspection.InjectsAnyOf(type, typeof(IUnitOfWork), typeof(IRepository))
            && !TypeInspection.ImplementsOpenGeneric(type, typeof(ICommandHandler<,>)))));

    private static bool IsHandler(Type type) =>
        TypeInspection.IsConcreteClass(type)
        && (TypeInspection.ImplementsOpenGeneric(type, typeof(ICommandHandler<,>))
            || TypeInspection.ImplementsOpenGeneric(type, typeof(IQueryHandler<,>)));
}
