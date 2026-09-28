using System.Text.RegularExpressions;
using EmergencyHub.ArchitectureTests.Samples.ControllerDependencies;
using Xunit.Sdk;

namespace EmergencyHub.ArchitectureTests;

// 대상 대기 목록(PendingTargetRules)과 안전장치. 원본: testing-strategy "대상 대기 목록" 표(S05-T02), 구현 S05-T04.
// 목록 규칙은 제품 대상 0개면 해제 작업 ID를 적어 건너뛰고, 대상이 생기면 "목록에서 빼라"로 실패한다. 목록 밖 규칙은 대상 0개면 그대로 실패한다.
[Trait("FR", "PRD-002/FR-01")]
[Trait("NFR", "PRD-002/NFR-06")]
public sealed partial class PendingTargetRuleTests
{
    // 표본 범위에는 규칙 대상(Validator · Controller)이 있으므로, 대기 규칙에 "제품 대상이 생긴" 상황을 재현한다.
    private static readonly Dictionary<string, Func<RuleCheck>> ChecksWithTargets = new(StringComparer.Ordinal)
    {
        [nameof(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories)] = () =>
            DependencyRules.ControllersDoNotUseInfrastructureOrRepositories.Check(RuleScope.Samples<SenderOnlySampleController>()),
    };

    private static readonly Dictionary<string, ArchitectureRule> RulesByName = new(StringComparer.Ordinal)
    {
        [nameof(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories)] = DependencyRules.ControllersDoNotUseInfrastructureOrRepositories,
    };

    // ---- 성공: 목록은 testing-strategy 표와 1:1 ----

    [Fact]
    public void All_IsExactlyTheDocumentedRulesWithReleaseTaskIds()
    {
        // S06-T04가 Validator 규칙 2개(ValidatorsDeriveFromRequestValidator · ValidatorsDoNotInjectRepositoriesOrServices)를 해제했다.
        PendingTargetRules.All.Select(pending => (pending.Rule, pending.ReleaseTaskId)).Should().Equal(
            (DependencyRules.ControllersDoNotUseInfrastructureOrRepositories, "S06-T05"));
        PendingTargetRules.All.Should().OnlyContain(pending => TaskId().IsMatch(pending.ReleaseTaskId));
        PendingTargetRules.All.Select(pending => pending.Rule).Should().OnlyHaveUniqueItems();
    }
    [Fact]
    public void ReleasedValidatorRules_AreOutsideListAndHaveProductTargets()
    {
        // S06-T04 해제: RegisterEmployeesCommandValidator가 생겨 대상 1개 이상 단언(ConventionRuleTests · 목록 밖 공허 통과 실패)으로 되돌아간다.
        ArchitectureRule[] released =
        [
            ConventionRules.ValidatorsDeriveFromRequestValidator,
            InjectionRules.ValidatorsDoNotInjectRepositoriesOrServices,
        ];

        foreach (var rule in released)
        {
            PendingTargetRules.Find(rule).Should().BeNull();
            var check = rule.CheckProduct();
            check.TargetNames.Should().Contain(name => name.EndsWith("RegisterEmployeesCommandValidator", StringComparison.Ordinal));
            check.ShouldPassOnProduct();
        }
    }


    [Theory]
    [InlineData(nameof(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories), "S06-T05")]
    public void ShouldPassOnProduct_PendingRuleWithoutProductTargets_SkipsWithReleaseTaskId(string ruleName, string releaseTaskId)
    {
        var check = RulesByName[ruleName].CheckProduct();

        var act = check.ShouldPassOnProduct;

        check.TargetNames.Should().BeEmpty("샘플 제거(S05-T04) 뒤 제품 대상이 없어야 대기 목록에 둘 수 있다");
        act.Should().Throw<SkipException>().Which.Message.Should().Contain($"해제 작업 {releaseTaskId}");
    }

    // ---- 실패: 안전장치 ----

    [Theory]
    [InlineData(nameof(DependencyRules.ControllersDoNotUseInfrastructureOrRepositories), "S06-T05")]
    public void ShouldPassOnProduct_PendingRuleWithTargets_FailsAskingToRemoveItFromList(string ruleName, string releaseTaskId)
    {
        var check = ChecksWithTargets[ruleName]();

        var act = check.ShouldPassOnProduct;

        check.TargetNames.Should().NotBeEmpty();
        var exception = act.Should().Throw<Exception>().Which;
        exception.Should().NotBeOfType<SkipException>("대상이 생긴 대기 규칙은 건너뛰지 않고 실패해야 한다");
        exception.Message.Should().Contain("목록에서 빼라").And.Contain(releaseTaskId);
    }

    // ---- 엣지: 목록 밖 규칙은 대상 0개면 그대로 실패 ----

    [Fact]
    public void ShouldPassOnProduct_SameRuleOutsideListWithoutTargets_FailsAsVacuousPass()
    {
        // 목록은 같은 인스턴스로 찾는다. 사본(with)은 목록 밖이므로 대상 0개면 공허 통과로 실패한다(대기 해제 뒤의 동작과 같다).
        var outsideList = DependencyRules.ControllersDoNotUseInfrastructureOrRepositories with { Name = "대기 목록 밖 사본" };
        var check = outsideList.CheckProduct();

        var act = check.ShouldPassOnProduct;

        PendingTargetRules.Find(outsideList).Should().BeNull();
        check.TargetNames.Should().BeEmpty();
        var exception = act.Should().Throw<Exception>().Which;
        exception.Should().NotBeOfType<SkipException>();
        exception.Message.Should().Contain("공허 통과");
    }

    [Fact]
    public void Find_RuleNotInList_ReturnsNull()
    {
        PendingTargetRules.Find(ConventionRules.RequestAndResponseModelsAreRecords).Should().BeNull(
            "도메인 이벤트(EmployeeRegisteredDomainEvent)가 남아 대상이 있으므로 대기 목록에 두지 않는다");
    }

    [GeneratedRegex(@"\AS\d{2}-T\d{2}\z", RegexOptions.CultureInvariant)]
    private static partial Regex TaskId();
}
