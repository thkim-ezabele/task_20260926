using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Cqrs;

// 캐시 판정 방법: 항목 수는 Count, "한 번만 만들었다"는 반환된 호출기의 참조 동일성(BeSameAs)으로 판정한다.
public sealed class RequestInvokerCacheTests
{
    // ---- 성공 ----

    [Fact]
    public void GetCommandInvoker_SameCommandTypeTwice_KeepsSingleEntryByCountAndSameInstanceByReference()
    {
        var cache = new RequestInvokerCache();

        var first = cache.GetCommandInvoker<Guid>(typeof(CreateSampleCommand));
        var second = cache.GetCommandInvoker<Guid>(typeof(CreateSampleCommand));

        cache.Count.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetQueryInvoker_SameQueryTypeTwice_KeepsSingleEntryByCountAndSameInstanceByReference()
    {
        var cache = new RequestInvokerCache();

        var first = cache.GetQueryInvoker<SampleResponse>(typeof(GetSampleQuery));
        var second = cache.GetQueryInvoker<SampleResponse>(typeof(GetSampleQuery));

        cache.Count.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetCommandInvoker_DifferentCommandTypes_AddsOneEntryPerTypeByCount()
    {
        var cache = new RequestInvokerCache();

        cache.GetCommandInvoker<Guid>(typeof(CreateSampleCommand));
        cache.GetCommandInvoker<Unit>(typeof(SampleCommand));
        cache.GetQueryInvoker<SampleResponse>(typeof(GetSampleQuery));

        cache.Count.Should().Be(3);
        cache.ContainsCommand(typeof(CreateSampleCommand), typeof(Guid)).Should().BeTrue();
        cache.ContainsCommand(typeof(SampleCommand), typeof(Unit)).Should().BeTrue();
        cache.ContainsQuery(typeof(GetSampleQuery), typeof(SampleResponse)).Should().BeTrue();
    }

    [Fact]
    public void Shared_ReadTwice_ReturnsSameInstanceByReference()
    {
        RequestInvokerCache.Shared.Should().BeSameAs(RequestInvokerCache.Shared);
    }

    // ---- 실패 ----

    [Fact]
    public void GetCommandInvoker_TypeNotImplementingCommandOfResponse_ThrowsArgumentExceptionAndAddsNoEntryByCount()
    {
        var cache = new RequestInvokerCache();

        var act = () => cache.GetCommandInvoker<Guid>(typeof(SampleCommand));

        act.Should().Throw<ArgumentException>().WithParameterName("commandType");
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void GetQueryInvoker_TypeNotImplementingQueryOfResponse_ThrowsArgumentExceptionAndAddsNoEntryByCount()
    {
        var cache = new RequestInvokerCache();

        var act = () => cache.GetQueryInvoker<SampleResponse>(typeof(CreateSampleCommand));

        act.Should().Throw<ArgumentException>().WithParameterName("queryType");
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void GetCommandInvoker_NullType_ThrowsArgumentNullException()
    {
        var cache = new RequestInvokerCache();

        var act = () => cache.GetCommandInvoker<Guid>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("commandType");
    }

    [Fact]
    public void GetQueryInvoker_NullType_ThrowsArgumentNullException()
    {
        var cache = new RequestInvokerCache();

        var act = () => cache.GetQueryInvoker<SampleResponse>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("queryType");
    }

    // ---- 엣지 ----

    [Fact]
    public async Task GetCommandInvoker_ConcurrentCallsForSameType_KeepsSingleEntryByCountAndAllSameInstanceByReference()
    {
        const int ConcurrentCalls = 64;
        var cache = new RequestInvokerCache();
        using var start = new ManualResetEventSlim(false);

        var calls = Enumerable.Range(0, ConcurrentCalls)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return cache.GetCommandInvoker<Guid>(typeof(CreateSampleCommand));
            }))
            .ToList();
        start.Set();
        var invokers = await Task.WhenAll(calls);

        cache.Count.Should().Be(1);
        invokers.Should().AllSatisfy(invoker => invoker.Should().BeSameAs(invokers[0]));
    }

    [Fact]
    public void GetCommandInvoker_SameTypeWithTwoResponseTypes_KeepsSeparateEntriesByCount()
    {
        var cache = new RequestInvokerCache();

        var asInt = cache.GetCommandInvoker<int>(typeof(DualResponseCommand));
        var asString = cache.GetCommandInvoker<string>(typeof(DualResponseCommand));

        cache.Count.Should().Be(2);
        asInt.Should().NotBeSameAs(asString);
    }

    [Fact]
    public void GetInvokers_SameTypeAsCommandAndQuery_KeepsSeparateEntriesByContains()
    {
        var cache = new RequestInvokerCache();

        cache.GetCommandInvoker<int>(typeof(CommandAndQueryRequest));

        cache.ContainsCommand(typeof(CommandAndQueryRequest), typeof(int)).Should().BeTrue();
        cache.ContainsQuery(typeof(CommandAndQueryRequest), typeof(int)).Should().BeFalse();
        cache.GetQueryInvoker<int>(typeof(CommandAndQueryRequest));
        cache.Count.Should().Be(2);
    }

    [Fact]
    public void GetCommandInvoker_CommandWithoutResponse_UsesUnitResponseEntry()
    {
        var cache = new RequestInvokerCache();

        cache.GetCommandInvoker<Unit>(typeof(SampleCommand));

        cache.ContainsCommand(typeof(SampleCommand), typeof(Unit)).Should().BeTrue();
    }
}
