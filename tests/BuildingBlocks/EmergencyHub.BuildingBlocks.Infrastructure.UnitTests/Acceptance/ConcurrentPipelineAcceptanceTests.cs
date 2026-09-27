using System.Collections.Concurrent;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// PRD-001 FR-05 엣지(동시성): 실제 DI 조합에서 여러 요청 스코프가 동시에 Send해도
// 스코프마다 자기 ISender · Handler 체인 · IUnitOfWork를 쓰고, 검증 실패는 커밋 없이 102로 한 번씩만 남는지 확인한다.
// 요청 1건 = 스코프 1개(ASP.NET Core 요청 스코프와 같은 모양). 스코프 안 중첩 · 동시 Send는 ADR-0015에서 금지이므로 다루지 않는다.
[Trait("FR", "PRD-001/FR-05")]
public sealed class ConcurrentPipelineAcceptanceTests : IDisposable
{
    private const int RequestCount = 64;

    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    private readonly CountingPipelineProbe _probe = new();
    private readonly ConcurrentBag<CountingUnitOfWork> _unitsOfWork = [];
    private readonly ServiceProvider _provider;

    public ConcurrentPipelineAcceptanceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFakeLogging());
        services.AddSingleton<IPipelineProbe>(_probe);
        services.AddScoped<IUnitOfWork>(_ =>
        {
            var unitOfWork = new CountingUnitOfWork();
            _unitsOfWork.Add(unitOfWork);
            return unitOfWork;
        });
        services.AddBuildingBlocksInfrastructure();
        services.AddConventionalServices(typeof(ConcurrentPipelineAcceptanceTests).Assembly);

        _provider = services.BuildServiceProvider(StrictOptions);
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task SendCommand_ParallelScopesAllValid_EachScopeCommitsExactlyOnceWithOwnUnitOfWork()
    {
        var senders = new ConcurrentBag<ISender>();

        var results = await Task.WhenAll(Enumerable.Range(1, RequestCount).Select(value => Task.Run(async () =>
        {
            await using var scope = _provider.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            senders.Add(sender);
            return await sender.SendAsync(new CreateSampleCommand(value), CancellationToken.None);
        })));

        results.Should().OnlyContain(result => result.IsSuccess);
        _probe.Validated.Should().Be(RequestCount);
        _probe.Handled.Should().Be(RequestCount);
        senders.Distinct().Should().HaveCount(RequestCount);
        _unitsOfWork.Should().HaveCount(RequestCount);
        _unitsOfWork.Should().OnlyContain(unitOfWork => unitOfWork.Commits == 1);
        MediatorRecords().Should().HaveCount(RequestCount).And.OnlyContain(record => record.Id.Id == 101);
    }

    [Fact]
    public async Task SendCommand_ParallelScopesMixedOutcomes_CommitsOnlySuccessesAndLogsEachFailureOnce()
    {
        // value % 4: 0 → 검증 실패(-value), 1 → Handler 실패(0), 나머지 → 성공.
        static int ValueFor(int i) => (i % 4) switch
        {
            0 => -i - 1,
            1 => 0,
            _ => i + 1,
        };

        var indexes = Enumerable.Range(0, RequestCount).ToList();
        var validationFailures = indexes.Count(i => i % 4 == 0);
        var handlerFailures = indexes.Count(i => i % 4 == 1);
        var successes = RequestCount - validationFailures - handlerFailures;

        var results = await Task.WhenAll(indexes.Select(i => Task.Run(async () =>
        {
            await using var scope = _provider.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            return await sender.SendAsync(new CreateSampleCommand(ValueFor(i)), CancellationToken.None);
        })));

        results.Count(result => result.IsSuccess).Should().Be(successes);
        results.Count(result => result.IsFailure && result.Error.Code == SampleErrors.ValueConflict.Code).Should().Be(handlerFailures);
        _probe.Validated.Should().Be(RequestCount);
        _probe.Handled.Should().Be(RequestCount - validationFailures);
        _unitsOfWork.Sum(unitOfWork => unitOfWork.Commits).Should().Be(successes);
        _unitsOfWork.Should().OnlyContain(unitOfWork => unitOfWork.Commits <= 1);
        var records = MediatorRecords();
        records.Count(record => record.Id.Id == 101).Should().Be(successes);
        records.Count(record => record.Id.Id == 102).Should().Be(validationFailures + handlerFailures);
    }

    [Fact]
    public async Task SendQuery_ParallelScopes_NeverResolvesUnitOfWork()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, RequestCount).Select(value => Task.Run(async () =>
        {
            await using var scope = _provider.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            return await sender.QueryAsync(new GetSampleQuery(value), CancellationToken.None);
        })));

        results.Should().OnlyContain(result => result.IsSuccess);
        _probe.Handled.Should().Be(RequestCount);
        _unitsOfWork.Should().BeEmpty();
        MediatorRecords().Should().HaveCount(RequestCount).And.OnlyContain(record => record.Id.Id == 103);
    }

    private IReadOnlyList<FakeLogRecord> MediatorRecords() =>
        [.. _provider.GetFakeLogCollector().GetSnapshot().Where(record => record.Id.Id is >= 101 and <= 199)];
}
