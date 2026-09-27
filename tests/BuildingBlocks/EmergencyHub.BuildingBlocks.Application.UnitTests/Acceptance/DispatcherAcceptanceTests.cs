using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Acceptance;

// PRD-001 FR-05 중 S02-T01 범위(직접 구현한 디스패처 · Handler 타입 캐시)의 인수 시나리오(tester 추가분).
// developer 단위 테스트(SenderTests · RequestInvokerCacheTests)가 다루지 않은 항목만 둔다:
// 인터페이스 형식 변수로 보낸 요청의 런타임 형식 해석, 한 인자 Handler 편의 인터페이스 구현체 디스패치,
// Query 쪽 동시성 · 취소, 여러 형식 동시 첫 요청의 캐시 항목 수.
// 파이프라인 순서 · 검증 실패 시 미호출 · 실패 Result 미커밋은 S02-T02 · T03이 검증한다.
[Trait("FR", "PRD-001/FR-05")]
public sealed class DispatcherAcceptanceTests
{
    // ---- 성공 ----

    [Fact]
    public async Task SendAsync_CommandDeclaredAsInterfaceVariable_DispatchesToRuntimeTypeHandlerAndCachesRuntimeTypeByContains()
    {
        var cache = new RequestInvokerCache();
        var createdId = Guid.NewGuid();
        var handler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        handler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(createdId));
        var sender = new Sender(new ServiceProviderStub().Add(handler), cache);
        ICommand<Guid> command = new CreateSampleCommand("홍길동");

        var result = await sender.SendAsync(command, CancellationToken.None);

        result.Value.Should().Be(createdId);
        cache.ContainsCommand(typeof(CreateSampleCommand), typeof(Guid)).Should().BeTrue();
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_HandlerImplementingSingleArgumentInterface_IsDispatchedWhenRegisteredAsTwoArgumentInterface()
    {
        var handler = new RecordingSampleCommandHandler();
        var sender = new Sender(
            new ServiceProviderStub().Add<ICommandHandler<SampleCommand, Unit>>(handler),
            new RequestInvokerCache());
        var command = new SampleCommand(3);

        var result = await sender.SendAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.Received.Should().ContainSingle().Which.Should().BeSameAs(command);
    }

    [Fact]
    public async Task SendAsync_UnitCommandHandlerReturnsFailure_ReturnsSameFailure()
    {
        var error = Error.BusinessRule(24002, "이미 처리된 요청입니다.");
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Unit>(error));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var result = await sender.SendAsync(new SampleCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(error);
    }

    // ---- 실패 ----

    [Fact]
    public async Task SendAsync_OnlyOtherResponseHandlerRegistered_ThrowsInvalidOperationExceptionContainingRequestTypeName()
    {
        var intHandler = Substitute.For<ICommandHandler<DualResponseCommand, int>>();
        intHandler.Handle(Arg.Any<DualResponseCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(1));
        var sender = new Sender(new ServiceProviderStub().Add(intHandler), new RequestInvokerCache());

        var act = () => sender.SendAsync<string>(new DualResponseCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain(typeof(DualResponseCommand).FullName);
        await intHandler.DidNotReceive().Handle(Arg.Any<DualResponseCommand>(), Arg.Any<CancellationToken>());
    }

    // ---- 엣지: 취소 ----

    [Fact]
    public async Task QueryAsync_WithAlreadyCanceledToken_PassesCanceledTokenToHandlerAndPropagatesCancellation()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<Result<SampleResponse>>(call.Arg<CancellationToken>()));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var act = () => sender.QueryAsync(new GetSampleQuery(1), source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await handler.Received(1).Handle(Arg.Any<GetSampleQuery>(), Arg.Is<CancellationToken>(token => token.IsCancellationRequested));
    }

    // ---- 엣지: 동시성 ----

    [Fact]
    public async Task QueryAsync_ConcurrentQueriesOfSameType_AllReturnOwnResponseAndCacheKeepsSingleEntryByCount()
    {
        const int ConcurrentQueries = 64;
        var cache = new RequestInvokerCache();
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(new SampleResponse(call.Arg<GetSampleQuery>().Id, "응답")));
        var provider = new ServiceProviderStub().Add(handler);
        using var start = new ManualResetEventSlim(false);

        var queries = Enumerable.Range(0, ConcurrentQueries)
            .Select(index => Task.Run(async () =>
            {
                start.Wait();
                var result = await new Sender(provider, cache).QueryAsync(new GetSampleQuery(index), CancellationToken.None);
                return (Index: index, Result: result);
            }))
            .ToList();
        start.Set();
        var results = await Task.WhenAll(queries);

        results.Should().OnlyContain(pair => pair.Result.IsSuccess && pair.Result.Value.Id == pair.Index);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task SendAndQuery_ConcurrentFirstRequestsOfDifferentTypes_CacheKeepsOneEntryPerRequestTypeByCount()
    {
        const int RepeatsPerType = 16;
        var cache = new RequestInvokerCache();
        var unitHandler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        unitHandler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var createHandler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        createHandler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        var queryHandler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        queryHandler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SampleResponse(1, "a")));
        var provider = new ServiceProviderStub().Add(unitHandler).Add(createHandler).Add(queryHandler);
        using var start = new ManualResetEventSlim(false);

        Func<Sender, Task<bool>>[] requests =
        [
            async sender => (await sender.SendAsync(new SampleCommand(1), CancellationToken.None)).IsSuccess,
            async sender => (await sender.SendAsync(new CreateSampleCommand("a"), CancellationToken.None)).IsSuccess,
            async sender => (await sender.QueryAsync(new GetSampleQuery(1), CancellationToken.None)).IsSuccess,
        ];
        var tasks = Enumerable.Range(0, RepeatsPerType)
            .SelectMany(_ => requests)
            .Select(request => Task.Run(async () =>
            {
                start.Wait();
                return await request(new Sender(provider, cache));
            }))
            .ToList();
        start.Set();
        var outcomes = await Task.WhenAll(tasks);

        outcomes.Should().HaveCount(RepeatsPerType * requests.Length).And.OnlyContain(success => success);
        cache.Count.Should().Be(requests.Length);
        cache.ContainsCommand(typeof(SampleCommand), typeof(Unit)).Should().BeTrue();
        cache.ContainsCommand(typeof(CreateSampleCommand), typeof(Guid)).Should().BeTrue();
        cache.ContainsQuery(typeof(GetSampleQuery), typeof(SampleResponse)).Should().BeTrue();
    }

    /// <summary>한 인자 편의 인터페이스(<see cref="ICommandHandler{TCommand}"/>)를 구현한 실제 Handler.</summary>
    private sealed class RecordingSampleCommandHandler : ICommandHandler<SampleCommand>
    {
        public List<SampleCommand> Received { get; } = [];

        public Task<Result<Unit>> Handle(SampleCommand command, CancellationToken cancellationToken)
        {
            Received.Add(command);
            return Task.FromResult(Result.Success(Unit.Value));
        }
    }
}
