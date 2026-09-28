using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace EmergencyHub.ArchitectureTests;

// 대상 대기 목록(PendingTargetRules)과 안전장치. 원본: testing-strategy "대상 대기 목록" 표(S05-T02), 구현 S05-T04.
// 목록 규칙은 제품 대상 0개면 해제 작업 ID를 적어 건너뛰고, 대상이 생기면 "목록에서 빼라"로 실패한다. 목록 밖 규칙은 대상 0개면 그대로 실패한다.
// S06-T05에서 마지막 규칙(ControllersDoNotUseInfrastructureOrRepositories)을 해제해 제품 목록은 비었다. 안전장치는 표본 목록(SampleList)으로 확인한다.
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-06")]
public sealed partial class PendingTargetRuleTests
{
    private const string ZeroTargetReleaseTaskId = "S99-T01";
    private const string ControllerReleaseTaskId = "S99-T02";

    // 제품 대상이 0개인 규칙(Controller 규칙의 대상 선택을 없는 형식 이름으로 바꾼 사본).
    private static readonly ArchitectureRule ZeroTargetRule = DependencyRules.ControllersDoNotUseInfrastructureOrRepositories with
    {
        Name = "대상 0개 표본(ControllersDoNotUseInfrastructureOrRepositories 사본)",
        SelectTargets = scope => scope.And().HaveName("NoSuchTypeForPendingTargetRuleTests"),
    };

    // 표본 대기 목록: 대상 0개 규칙 하나와, 대상(EmployeeController)이 생긴 규칙 하나.
    private static readonly IReadOnlyList<PendingTargetRule> SampleList =
    [
        new(ZeroTargetRule, ZeroTargetReleaseTaskId),
        new(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories, ControllerReleaseTaskId),
    ];

    // ---- 성공: 목록은 testing-strategy 표와 1:1(0행), 해제한 규칙은 대상이 있다 ----

    [Fact]
    public void All_IsEmptyLikeTheDocumentedTable()
    {
        // S06-T04가 Validator 규칙 2개, S06-T05가 Controller 규칙 1개를 해제했다.
        PendingTargetRules.All.Should().BeEmpty();
        SampleList.Should().OnlyContain(pending => TaskId().IsMatch(pending.ReleaseTaskId));
    }

    [Theory]
    [InlineData(nameof(ConventionRules.ValidatorsDeriveFromRequestValidator), "RegisterEmployeesCommandValidator")]
    [InlineData(nameof(InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices), "RegisterEmployeesCommandValidator")]
    [InlineData(nameof(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories), "EmployeeController")]
    public void ReleasedRules_AreOutsideListAndHaveProductTargets(string ruleName, string targetTypeName)
    {
        // 해제한 규칙은 대상 1개 이상 단언(목록 밖 공허 통과 실패)으로 되돌아간다. S06-T04: Validator 2개, S06-T05: Controller 1개.
        var rule = ReleasedRule(ruleName);

        var check = rule.CheckProduct();

        PendingTargetRules.Find(rule).Should().BeNull();
        check.TargetNames.Should().Contain(name => name.EndsWith("." + targetTypeName, StringComparison.Ordinal));
        check.ShouldPassOnProduct();
    }

    [Fact]
    public void ShouldPassOnProduct_PendingRuleWithoutProductTargets_SkipsWithReleaseTaskId()
    {
        var check = ZeroTargetRule.CheckProduct();

        var act = () => check.ShouldPassOnProduct(SampleList);

        check.TargetNames.Should().BeEmpty();
        act.Should().Throw<SkipException>().Which.Message.Should().Contain($"해제 작업 {ZeroTargetReleaseTaskId}");
    }

    // ---- 실패: 안전장치 ----

    [Fact]
    public void ShouldPassOnProduct_PendingRuleWithProductTargets_FailsAskingToRemoveItFromList()
    {
        // S06-T05 해제 직전 상태의 재현: 목록에 남은 Controller 규칙에 EmployeeController가 생기면 건너뛰지 않고 실패한다.
        var check = DependencyRules.ControllersDoNotUseInfrastructureOrRepositories.CheckProduct();

        var act = () => check.ShouldPassOnProduct(SampleList);

        check.TargetNames.Should().NotBeEmpty();
        var exception = act.Should().Throw<Exception>().Which;
        exception.Should().NotBeOfType<SkipException>("대상이 생긴 대기 규칙은 건너뛰지 않고 실패해야 한다");
        exception.Message.Should().Contain("목록에서 빼라").And.Contain(ControllerReleaseTaskId);
    }

    [Fact]
    public void ShouldPassOnProduct_SameRuleOutsideListWithoutTargets_FailsAsVacuousPass()
    {
        // 제품 목록(비어 있음)으로 판정하면 대상 0개 규칙은 공허 통과로 실패한다(대기 해제 뒤의 동작과 같다).
        var check = ZeroTargetRule.CheckProduct();

        Action act = check.ShouldPassOnProduct;

        PendingTargetRules.Find(ZeroTargetRule).Should().BeNull();
        var exception = act.Should().Throw<Exception>().Which;
        exception.Should().NotBeOfType<SkipException>();
        exception.Message.Should().Contain("공허 통과");
    }

    // ---- 엣지: 목록은 같은 인스턴스로 찾는다 ----

    [Fact]
    public void Find_CopyOfListedRule_ReturnsNull()
    {
        var copy = DependencyRules.ControllersDoNotUseInfrastructureOrRepositories with { Name = "대기 목록 밖 사본" };

        PendingTargetRules.Find(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories, SampleList)!.ReleaseTaskId.Should().Be(ControllerReleaseTaskId);
        PendingTargetRules.Find(copy, SampleList).Should().BeNull();
        PendingTargetRules.Find(ConventionRules.RequestAndResponseModelsAreRecords).Should().BeNull();
    }

    private static ArchitectureRule ReleasedRule(string ruleName) => ruleName switch
    {
        nameof(ConventionRules.ValidatorsDeriveFromRequestValidator) => ConventionRules.ValidatorsDeriveFromRequestValidator,
        nameof(InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices) => InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices,
        _ => DependencyRules.ControllersDoNotUseInfrastructureOrRepositories,
    };

    [GeneratedRegex(@"\AS\d{2}-T\d{2}\z", RegexOptions.CultureInvariant)]
    private static partial Regex TaskId();
}
