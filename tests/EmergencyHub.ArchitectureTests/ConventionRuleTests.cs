namespace EmergencyHub.ArchitectureTests;

// 컨벤션 · 주입 규칙을 제품 어셈블리(ArchitectureAssemblies)에 적용한다. 규칙마다 테스트 1개, 대상 형식 1개 이상 단언.
// 대상이 서비스 코드에만 있는 규칙(TargetsOnlyInServices)은 서비스 어셈블리가 목록에 없는 동안(S03 전) 건너뜀으로 표시된다.
// 대상 대기 목록(PendingTargetRules, S05-T04)의 규칙은 대상 0개면 해제 작업 ID를 적어 건너뛰고, 대상이 생기면 실패한다(S06-T05부터 목록은 비어 있음).
// 규칙 원본은 ConventionRules · InjectionRules의 Source. 위반 예시는 ConventionRuleSampleTests.
[Trait("FR", "PRD-001/FR-09")]
[Trait("NFR", "PRD-001/NFR-02")]
public sealed class ConventionRuleTests
{
    // coding-conventions "C# 언어 기능" 클래스는 기본 sealed, FR-09(sealed)
    [Fact]
    public void ClassesAreSealed_ProductAssemblies_Holds() =>
        ConventionRules.ClassesAreSealed.CheckProduct().ShouldPassOnProduct();

    // coding-conventions record 행, FR-09(Command / Query / Response는 record) — 대상은 서비스 코드(S03)
    [Fact]
    public void RequestAndResponseModelsAreRecords_ProductAssemblies_Holds() =>
        ConventionRules.RequestAndResponseModelsAreRecords.CheckProduct().ShouldPassOnProduct();

    // coding-conventions DI 규칙 마커 표, FR-09(Repository 인터페이스의 마커 상속) — 대상은 서비스 코드(S03)
    [Fact]
    public void RepositoryInterfacesInheritMarkers_ProductAssemblies_Holds() =>
        ConventionRules.RepositoryInterfacesInheritMarkers.CheckProduct().ShouldPassOnProduct();

    // coding-conventions DI 규칙 마커 표(구현은 RepositoryBase / ReadRepositoryBase) — 대상은 서비스 코드(S03)
    [Fact]
    public void RepositoryImplementationsDeriveFromBases_ProductAssemblies_Holds() =>
        ConventionRules.RepositoryImplementationsDeriveFromBases.CheckProduct().ShouldPassOnProduct();

    // BL-072 — 대상은 서비스 코드(S03)
    [Fact]
    public void StronglyTypedIdsReferenceThemselves_ProductAssemblies_Holds() =>
        ConventionRules.StronglyTypedIdsReferenceThemselves.CheckProduct().ShouldPassOnProduct();

    // coding-conventions 강타입 ID(record struct · IStronglyTypedId<TSelf>) — 대상은 서비스 코드(S03)
    [Fact]
    public void EntityIdsAreStronglyTyped_ProductAssemblies_Holds() =>
        ConventionRules.EntityIdsAreStronglyTyped.CheckProduct().ShouldPassOnProduct();

    // coding-conventions "코드값 (enum) 규칙" 기반 형식, ADR-0008
    [Fact]
    public void CodeEnumsUseConventionalUnderlyingTypes_ProductAssemblies_Holds() =>
        ConventionRules.CodeEnumsUseConventionalUnderlyingTypes.CheckProduct().ShouldPassOnProduct();

    // coding-conventions CQRS · Validator · Repository 예시, ADR-0015(구현 internal)
    [Fact]
    public void ImplementationsAreInternalSealed_ProductAssemblies_Holds() =>
        ConventionRules.ImplementationsAreInternalSealed.CheckProduct().ShouldPassOnProduct();

    // coding-conventions Validator 규칙, ADR-0018 — 대상은 서비스 코드(RegisterEmployeesCommandValidator). 대상 대기는 S06-T04에서 해제
    [Fact]
    public void ValidatorsDeriveFromRequestValidator_ProductAssemblies_Holds() =>
        ConventionRules.ValidatorsDeriveFromRequestValidator.CheckProduct().ShouldPassOnProduct();

    // coding-conventions 공통 등록 진입점 표(명시 등록 포트)
    [Fact]
    public void ExplicitlyRegisteredPortsDoNotImplementMarkers_ProductAssemblies_Holds() =>
        ConventionRules.ExplicitlyRegisteredPortsDoNotImplementMarkers.CheckProduct().ShouldPassOnProduct();

    // BL-029(BL-054 병합), TD-016
    [Fact]
    public void ErrorAndResultAreNotDerived_ProductAssemblies_Holds() =>
        ConventionRules.ErrorAndResultAreNotDerived.CheckProduct().ShouldPassOnProduct();

    // BL-029(BL-054 병합) — 대상은 서비스 코드(S03)
    [Fact]
    public void EntityDerivedTypesAreSealed_ProductAssemblies_Holds() =>
        ConventionRules.EntityDerivedTypesAreSealed.CheckProduct().ShouldPassOnProduct();

    // coding-conventions CQRS 규칙, ADR-0015, BL-029
    [Fact]
    public void HandlersDoNotDependOnSender_ProductAssemblies_Holds() =>
        InjectionRules.HandlersDoNotDependOnSender.CheckProduct().ShouldPassOnProduct();

    // ADR-0018(Validator DB 접근 금지), BL-029 — 대상은 서비스 코드(RegisterEmployeesCommandValidator). 대상 대기는 S06-T04에서 해제
    [Fact]
    public void ValidatorsDoNotInjectRepositoriesOrServices_ProductAssemblies_Holds() =>
        InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices.CheckProduct().ShouldPassOnProduct();

    // coding-conventions CQRS 규칙, ADR-0007 · 0009 · 0014
    [Fact]
    public void QueryHandlersDoNotUseWriteSide_ProductAssemblies_Holds() =>
        InjectionRules.QueryHandlersDoNotUseWriteSide.CheckProduct().ShouldPassOnProduct();
}
