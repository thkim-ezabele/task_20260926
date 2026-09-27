using System.Globalization;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Acceptance;

// PRD-001 FR-05 순서 조건을 실제 DI 조합으로 닫는다(S02-T02 tester 인계):
// AddBuildingBlocksInfrastructure() + AddConventionalServices로 해석한 ISender에서
// 검증 실패 → Handler · CommitAsync 0회 + 이벤트 102 1건, Validator → Handler → CommitAsync 순서, Query 체인은 IUnitOfWork 미해석.
// 기대 체인의 기준은 Application.UnitTests의 DecoratorChainAcceptanceTests.CreateCommandChain(수동 조립)이다.
[Trait("FR", "PRD-001/FR-05")]
public sealed class PipelineOrderAcceptanceTests : IDisposable
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    private readonly IPipelineProbe _probe = Substitute.For<IPipelineProbe>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private int _unitOfWorkResolutions;

    public PipelineOrderAcceptanceTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFakeLogging());
        services.AddScoped(_ => _probe);
        services.AddScoped(_ =>
        {
            Interlocked.Increment(ref _unitOfWorkResolutions);
            return _unitOfWork;
        });
        services.AddBuildingBlocksInfrastructure();
        services.AddConventionalServices(typeof(PipelineOrderAcceptanceTests).Assembly);

        _provider = services.BuildServiceProvider(StrictOptions);
        _scope = _provider.CreateScope();
    }

    private ISender Sender => _scope.ServiceProvider.GetRequiredService<ISender>();

    private FakeLogCollector Logs => _provider.GetFakeLogCollector();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    // ---- 성공: Command ----

    [Fact]
    public async Task SendCommand_AllStagesSucceed_RunsValidatorHandlerCommitInOrderAndLogsOneDebugRecord()
    {
        var command = new CreateSampleCommand(5);

        var result = await Sender.SendAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(CreateSampleCommandHandler.CreatedId);
        Received.InOrder(() =>
        {
            _probe.Validating(5);
            _probe.Handling(command);
            _unitOfWork.CommitAsync(CancellationToken.None);
        });
        var record = MediatorRecords().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(101);
        record.Level.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public async Task SendUnitCommand_OneArgumentHandler_GoesThroughTransactionAndCommits()
    {
        var command = new ArchiveSampleCommand(1);

        var result = await Sender.SendAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _probe.Received(1).Handling(command);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        MediatorRecords().Should().ContainSingle().Which.Id.Id.Should().Be(101);
    }

    [Fact]
    public async Task SendCommand_WithToken_PassesSameTokenToCommit()
    {
        using var cts = new CancellationTokenSource();

        await Sender.SendAsync(new CreateSampleCommand(5), cts.Token);

        await _unitOfWork.Received(1).CommitAsync(cts.Token);
    }

    // ---- 실패: Command ----

    [Fact]
    public async Task SendCommand_ValidationFails_DoesNotCallHandlerOrCommitAndLogsEvent102Once()
    {
        var result = await Sender.SendAsync(new CreateSampleCommand(-1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        var error = result.Error.Should().BeOfType<ValidationError>().Subject;
        error.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        error.Errors.Select(field => field.Code).Should().Equal(SampleErrors.ValueNegative.Code);
        _probe.DidNotReceive().Handling(Arg.Any<object>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        var record = MediatorRecords().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.Level.Should().Be(LogLevel.Information);
        record.GetStructuredStateValue("ErrorCode").Should().Be(CommonErrors.ValidationFailed.Code.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SendCommand_HandlerReturnsFailure_DoesNotCommitAndLogsHandlerCode()
    {
        var result = await Sender.SendAsync(new CreateSampleCommand(0), CancellationToken.None);

        result.Error.Should().BeSameAs(SampleErrors.ValueConflict);
        _probe.Received(1).Handling(Arg.Any<object>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        var record = MediatorRecords().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.GetStructuredStateValue("ErrorCode").Should().Be("23001");
    }

    [Fact]
    public async Task SendCommand_CommitReturnsFailure_ReturnsCommitErrorAndLogsItOnce()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await Sender.SendAsync(new CreateSampleCommand(5), CancellationToken.None);

        result.Error.Should().BeSameAs(CommonErrors.ConcurrencyConflict);
        var record = MediatorRecords().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(102);
        record.GetStructuredStateValue("ErrorCode").Should().Be(CommonErrors.ConcurrencyConflict.Code.ToString(CultureInfo.InvariantCulture));
    }

    // ---- 성공 · 실패: Query(트랜잭션 없음) ----

    [Fact]
    public async Task SendQuery_Succeeds_RunsValidatorThenHandlerWithoutResolvingUnitOfWork()
    {
        var query = new GetSampleQuery(3);

        var result = await Sender.QueryAsync(query, CancellationToken.None);

        result.Value.Should().Be("sample-3");
        Received.InOrder(() =>
        {
            _probe.Validating(3);
            _probe.Handling(query);
        });
        _unitOfWorkResolutions.Should().Be(0);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        MediatorRecords().Should().ContainSingle().Which.Id.Id.Should().Be(103);
    }

    [Fact]
    public async Task SendQuery_ValidationFails_DoesNotCallHandlerAndLogsEvent104Once()
    {
        var result = await Sender.QueryAsync(new GetSampleQuery(-1), CancellationToken.None);

        result.Error.Should().BeOfType<ValidationError>();
        _probe.DidNotReceive().Handling(Arg.Any<object>());
        _unitOfWorkResolutions.Should().Be(0);
        var record = MediatorRecords().Should().ContainSingle().Subject;
        record.Id.Id.Should().Be(104);
        record.Level.Should().Be(LogLevel.Information);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task SendCommand_HandlerThrows_PropagatesExceptionWithoutCommitOrMediatorLog()
    {
        var exception = new InvalidOperationException("예상하지 못한 오류");
        _probe.When(p => p.Handling(Arg.Any<object>())).Do(_ => throw exception);

        var act = () => Sender.SendAsync(new CreateSampleCommand(5), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        MediatorRecords().Should().BeEmpty();
    }

    [Fact]
    public async Task SendCommand_TwiceInSameScope_CommitsOncePerCommand()
    {
        // 데코레이터가 한 겹씩만 적용됐는지(두 번 감싸면 커밋 · 로그가 두 배) 확인한다.
        await Sender.SendAsync(new CreateSampleCommand(1), CancellationToken.None);
        await Sender.SendAsync(new CreateSampleCommand(2), CancellationToken.None);

        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
        _probe.Received(2).Validating(Arg.Any<object>());
        MediatorRecords().Should().HaveCount(2);
    }

    // Mediator 이벤트 ID 범위(101~199)만 본다(다른 로그가 섞여도 판정이 흔들리지 않게).
    private IReadOnlyList<FakeLogRecord> MediatorRecords() =>
        [.. Logs.GetSnapshot().Where(record => record.Id.Id is >= 101 and <= 199)];
}
