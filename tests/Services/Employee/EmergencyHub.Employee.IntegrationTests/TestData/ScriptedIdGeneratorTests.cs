namespace EmergencyHub.Employee.IntegrationTests.TestData;

// S06-T06 도구(developer): 고정 IIdGenerator 대역.
public sealed class ScriptedIdGeneratorTests
{
    private static readonly Guid First = Guid.Parse("0192f0a0-0000-7000-8000-000000000001");
    private static readonly Guid Second = Guid.Parse("0192f0a0-0000-7000-8000-000000000002");

    // ---- 성공 ----

    [Fact]
    public void NewId_AfterRewind_ReturnsSameIdsFromStart()
    {
        var generator = new ScriptedIdGenerator([First, Second]);

        var firstRun = new[] { generator.NewId(), generator.NewId() };
        generator.Rewind();
        var secondRun = new[] { generator.NewId(), generator.NewId() };

        firstRun.Should().Equal(First, Second);
        secondRun.Should().Equal(firstRun, "재전송 시나리오는 두 요청이 같은 ID를 받는다");
    }

    // ---- 실패 ----

    [Fact]
    public void NewId_Exhausted_ThrowsInvalidOperation()
    {
        var generator = new ScriptedIdGenerator([First]);
        generator.NewId();

        var act = generator.NewId;

        act.Should().Throw<InvalidOperationException>().WithMessage("*Rewind*");
    }

    [Fact]
    public void Constructor_EmptyOrContainsEmptyGuid_Throws()
    {
        var empty = () => new ScriptedIdGenerator([]);
        var withEmpty = () => new ScriptedIdGenerator([First, Guid.Empty]);

        empty.Should().Throw<ArgumentException>();
        withEmpty.Should().Throw<ArgumentException>("운영 계약상 IIdGenerator는 빈 값을 돌려주지 않는다");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task NewId_ConcurrentCallers_IssueEachIdExactlyOnce()
    {
        var generator = ScriptedIdGenerator.WithRandomIds(1000);

        var issued = await Task.WhenAll(Enumerable.Range(0, 1000).Select(_ => Task.Run(generator.NewId, TestContext.Current.CancellationToken)));

        issued.Should().OnlyHaveUniqueItems();
        generator.Issued.Should().Be(1000);
    }
}
