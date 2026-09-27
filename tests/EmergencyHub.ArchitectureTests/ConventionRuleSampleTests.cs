using EmergencyHub.ArchitectureTests.Samples.EntityIds;
using EmergencyHub.ArchitectureTests.Samples.EntitySealing;
using EmergencyHub.ArchitectureTests.Samples.EnumTypes;
using EmergencyHub.ArchitectureTests.Samples.ErrorDerivation;
using EmergencyHub.ArchitectureTests.Samples.ExplicitPorts;
using EmergencyHub.ArchitectureTests.Samples.HandlerSender;
using EmergencyHub.ArchitectureTests.Samples.ImplementationVisibility;
using EmergencyHub.ArchitectureTests.Samples.QueryWriteSide;
using EmergencyHub.ArchitectureTests.Samples.RecordModels;
using EmergencyHub.ArchitectureTests.Samples.RepositoryImplementations;
using EmergencyHub.ArchitectureTests.Samples.RepositoryInterfaces;
using EmergencyHub.ArchitectureTests.Samples.SealedClasses;
using EmergencyHub.ArchitectureTests.Samples.StronglyTypedIds;
using EmergencyHub.ArchitectureTests.Samples.ValidatorBases;
using EmergencyHub.ArchitectureTests.Samples.ValidatorInjection;

namespace EmergencyHub.ArchitectureTests;

// 컨벤션 · 주입 규칙이 일부러 어긴 표본 형식만 잡는지 확인한다(ConventionRuleTests와 같은 규칙 객체, 범위만 표본 네임스페이스).
// 서비스 전용 규칙(S03 전 제품 검사는 건너뜀)은 이 표본 테스트가 규칙 동작의 근거다: 지킨 형식은 통과, 어긴 형식만 실패.
[Trait("FR", "PRD-001/FR-09")]
public sealed class ConventionRuleSampleTests
{
    [Fact]
    public void ClassesAreSealed_SamplesWithOpenConcreteClass_FlagsOnlyItAndSkipsAbstractAndStatic() =>
        ConventionRules.ClassesAreSealed.Check(RuleScope.Samples<SealedSample>())
            .ShouldFlagExactly(typeof(OpenSample));

    [Fact]
    public void RequestAndResponseModelsAreRecords_SamplesSelectedByInterfaceOrNameSuffix_FlagsNonRecords() =>
        ConventionRules.RequestAndResponseModelsAreRecords.Check(RuleScope.Samples<RecordCommand>())
            .ShouldFlagExactly(typeof(ClassCommand), typeof(ClassResponse), typeof(StructSummaryDto), typeof(ClassBasedNotice));

    [Fact]
    public void RepositoryInterfacesInheritMarkers_SamplesUnmarkedOrMismarked_FlagsBoth() =>
        ConventionRules.RepositoryInterfacesInheritMarkers.Check(RuleScope.Samples<ISampleOrderRepository>())
            .ShouldFlagExactly(typeof(IUnmarkedOrderRepository), typeof(IMismarkedOrderReadRepository));

    [Fact]
    public void RepositoryImplementationsDeriveFromBases_SamplesWithoutBaseOrWithCrossedBase_FlagsBoth() =>
        ConventionRules.RepositoryImplementationsDeriveFromBases.Check(RuleScope.Samples<SampleWriteRepository>())
            .ShouldFlagExactly(typeof(StandaloneWriteRepository), typeof(CrossedReadRepository));

    [Fact]
    public void StronglyTypedIdsReferenceThemselves_SampleBorrowingAnotherIdAsTSelf_FlagsOnlyIt() =>
        ConventionRules.StronglyTypedIdsReferenceThemselves.Check(RuleScope.Samples<OrderId>())
            .ShouldFlagExactly(typeof(BorrowedId));

    [Fact]
    public void EntityIdsAreStronglyTyped_SampleKeyedByRawGuid_FlagsOnlyIt() =>
        ConventionRules.EntityIdsAreStronglyTyped.Check(RuleScope.Samples<StronglyKeyedEntity>())
            .ShouldFlagExactly(typeof(GuidKeyedEntity));

    [Fact]
    public void CodeEnumsUseConventionalUnderlyingTypes_SamplesWithImplicitIntByteFlagsOrLongCode_FlagsEach() =>
        ConventionRules.CodeEnumsUseConventionalUnderlyingTypes.Check(RuleScope.Samples<SampleStatus>())
            .ShouldFlagExactly(typeof(ImplicitIntStatus), typeof(ByteChannels), typeof(ShortFlagsLikeStatus));

    [Fact]
    public void ImplementationsAreInternalSealed_SamplesPublicOrOpenImplementations_FlagsEach() =>
        ConventionRules.ImplementationsAreInternalSealed.Check(RuleScope.Samples<PublicSampleClassifier>())
            .ShouldFlagExactly(typeof(PublicSampleClassifier), typeof(PublicSampleCommandHandler), typeof(OpenPublicIdGenerator));

    [Fact]
    public void ValidatorsDeriveFromRequestValidator_SampleDerivingAbstractValidatorDirectly_FlagsOnlyIt() =>
        ConventionRules.ValidatorsDeriveFromRequestValidator.Check(RuleScope.Samples<PlainSampleCommandValidator>())
            .ShouldFlagExactly(typeof(PlainSampleCommandValidator));

    [Fact]
    public void ExplicitlyRegisteredPortsDoNotImplementMarkers_SamplesAlsoImplementingMarkers_FlagsBoth() =>
        ConventionRules.ExplicitlyRegisteredPortsDoNotImplementMarkers.Check(RuleScope.Samples<PlainSampleUnitOfWork>())
            .ShouldFlagExactly(typeof(MarkedSampleClassifier), typeof(MarkedSamplePreCommitHook));

    [Fact]
    public void ErrorAndResultAreNotDerived_SampleDerivingErrorThroughCopyConstructor_FlagsOnlyIt() =>
        ConventionRules.ErrorAndResultAreNotDerived.Check(RuleScope.Samples<PlainSampleClass>())
            .ShouldFlagExactly(typeof(CustomError));

    [Fact]
    public void EntityDerivedTypesAreSealed_SamplesOpenEntityOrAbstractIntermediateAggregate_FlagsBoth() =>
        ConventionRules.EntityDerivedTypesAreSealed.Check(RuleScope.Samples<SealedSampleEntity>())
            .ShouldFlagExactly(typeof(OpenSampleEntity), typeof(AbstractSampleAggregate));

    [Fact]
    public void HandlersDoNotDependOnSender_SamplesInjectingSender_FlagsCommandAndQueryHandlers() =>
        InjectionRules.HandlersDoNotDependOnSender.Check(RuleScope.Samples<PlainSampleCommandHandler>())
            .ShouldFlagExactly(typeof(SenderInjectedCommandHandler), typeof(SenderInjectedQueryHandler));

    [Fact]
    public void ValidatorsDoNotInjectRepositoriesOrServices_SamplesInjectingRepositoryOrService_FlagsEach() =>
        InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices.Check(RuleScope.Samples<TimeAwareSampleValidator>())
            .ShouldFlagExactly(typeof(WriteRepositoryInjectedValidator), typeof(ReadRepositoryInjectedValidator), typeof(ServiceInjectedValidator));

    [Fact]
    public void QueryHandlersDoNotUseWriteSide_SamplesUsingUnitOfWorkWriteRepositoryOrCommandHandler_FlagsEach() =>
        InjectionRules.QueryHandlersDoNotUseWriteSide.Check(RuleScope.Samples<ReadRepositorySampleQueryHandler>())
            .ShouldFlagExactly(typeof(UnitOfWorkSampleQueryHandler), typeof(WriteRepositorySampleQueryHandler), typeof(DualSampleHandler));
}
