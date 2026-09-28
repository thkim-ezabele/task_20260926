using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Cqrs;

public sealed class SenderTests
{
    private static readonly Error SampleError = Error.BusinessRule(24001, "허용되지 않은 상태 전이입니다.");

    // ---- 성공 ----

    [Fact]
    public async Task SendAsync_CommandWithoutResponse_CallsRegisteredHandlerWithSameCommandAndReturnsUnitSuccess()
    {
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());
        var command = new SampleCommand(1);

        // ICommand만 구현한 요청도 TResponse = Unit으로 추론되어 같은 SendAsync로 보낸다(ADR-0015).
        Result<Unit> result = await sender.SendAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        await handler.Received(1).Handle(Arg.Is<SampleCommand>(received => ReferenceEquals(received, command)), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_CommandWithResponse_ReturnsValueFromHandler()
    {
        var createdId = Guid.NewGuid();
        var handler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        handler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(createdId));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var result = await sender.SendAsync(new CreateSampleCommand("홍길동"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(createdId);
    }

    [Fact]
    public async Task QueryAsync_Query_ReturnsResponseFromHandler()
    {
        var response = new SampleResponse(7, "홍길동");
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());
        var query = new GetSampleQuery(7);

        var result = await sender.QueryAsync(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(response);
        await handler.Received(1).Handle(Arg.Is<GetSampleQuery>(received => ReferenceEquals(received, query)), CancellationToken.None);
    }

    // ---- 실패 ----

    [Fact]
    public async Task SendAsync_HandlerReturnsFailure_ReturnsSameFailureWithoutChange()
    {
        var handler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        handler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>(SampleError));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var result = await sender.SendAsync(new CreateSampleCommand("홍길동"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(SampleError);
    }

    [Fact]
    public async Task QueryAsync_HandlerReturnsFailure_ReturnsSameFailureWithoutChange()
    {
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<SampleResponse>(CommonErrors.NotFound));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var result = await sender.QueryAsync(new GetSampleQuery(404), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(CommonErrors.NotFound);
    }

    [Fact]
    public async Task SendAsync_HandlerNotRegistered_ThrowsInvalidOperationExceptionContainingRequestTypeName()
    {
        var sender = new Sender(new ServiceProviderStub(), new RequestInvokerCache());

        var act = () => sender.SendAsync(new UnregisteredCommand(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain(typeof(UnregisteredCommand).FullName);
    }

    [Fact]
    public async Task QueryAsync_HandlerNotRegistered_ThrowsInvalidOperationExceptionContainingRequestTypeName()
    {
        var sender = new Sender(new ServiceProviderStub(), new RequestInvokerCache());

        var act = () => sender.QueryAsync(new UnregisteredQuery(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain(typeof(UnregisteredQuery).FullName);
    }

    [Fact]
    public async Task SendAsync_HandlerMissingThenRegistered_DispatchesBecauseMissingHandlerIsNotCached()
    {
        var cache = new RequestInvokerCache();
        var missing = () => new Sender(new ServiceProviderStub(), cache).SendAsync(new CreateSampleCommand("홍길동"), CancellationToken.None);
        await missing.Should().ThrowAsync<InvalidOperationException>();
        var handler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        handler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        var result = await new Sender(new ServiceProviderStub().Add(handler), cache)
            .SendAsync(new CreateSampleCommand("홍길동"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_NullCommand_ThrowsArgumentNullException()
    {
        var sender = new Sender(new ServiceProviderStub(), new RequestInvokerCache());

        var act = () => sender.SendAsync<Unit>(null!, CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentNullException>()).WithParameterName("command");
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        var sender = new Sender(new ServiceProviderStub(), new RequestInvokerCache());

        var act = () => sender.QueryAsync<SampleResponse>(null!, CancellationToken.None);

        (await act.Should().ThrowAsync<ArgumentNullException>()).WithParameterName("query");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new Sender(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new Sender(new ServiceProviderStub(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public async Task SendAsync_HandlerThrows_PropagatesSameExceptionWithoutWrapping()
    {
        var thrown = new TimeoutException("DB 응답 없음");
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns<Task<Result<Unit>>>(_ => throw thrown);
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var act = () => sender.SendAsync(new SampleCommand(1), CancellationToken.None);

        (await act.Should().ThrowAsync<TimeoutException>()).Which.Should().BeSameAs(thrown);
    }

    [Fact]
    public async Task QueryAsync_HandlerThrows_PropagatesSameExceptionWithoutWrapping()
    {
        var thrown = new InvalidOperationException("읽기 실패");
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>()).Returns<Task<Result<SampleResponse>>>(_ => throw thrown);
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var act = () => sender.QueryAsync(new GetSampleQuery(1), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(thrown);
    }

    // ---- 엣지: CancellationToken 전달 ----

    [Fact]
    public async Task SendAsync_WithCancellationToken_PassesSameTokenToHandler()
    {
        using var source = new CancellationTokenSource();
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        await sender.SendAsync(new SampleCommand(1), source.Token);

        await handler.Received(1).Handle(Arg.Any<SampleCommand>(), source.Token);
    }

    [Fact]
    public async Task QueryAsync_WithCancellationToken_PassesSameTokenToHandler()
    {
        using var source = new CancellationTokenSource();
        var handler = Substitute.For<IQueryHandler<GetSampleQuery, SampleResponse>>();
        handler.Handle(Arg.Any<GetSampleQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new SampleResponse(1, "a")));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        await sender.QueryAsync(new GetSampleQuery(1), source.Token);

        await handler.Received(1).Handle(Arg.Any<GetSampleQuery>(), source.Token);
    }

    [Fact]
    public async Task SendAsync_WithAlreadyCanceledToken_PassesCanceledTokenToHandlerAndLeavesCancellationToHandler()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<Result<Unit>>(call.Arg<CancellationToken>()));
        var sender = new Sender(new ServiceProviderStub().Add(handler), new RequestInvokerCache());

        var act = () => sender.SendAsync(new SampleCommand(1), source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await handler.Received(1).Handle(Arg.Any<SampleCommand>(), Arg.Is<CancellationToken>(token => token.IsCancellationRequested));
    }

    // ---- 엣지: 캐시 · 스코프 · 동시성 ----

    [Fact]
    public async Task SendAsync_ConcurrentSendsOfSameCommandType_AllSucceedAndCacheKeepsSingleEntryByCount()
    {
        const int ConcurrentSends = 64;
        var cache = new RequestInvokerCache();
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var provider = new ServiceProviderStub().Add(handler);
        using var start = new ManualResetEventSlim(false);

        var sends = Enumerable.Range(0, ConcurrentSends)
            .Select(index => Task.Run(async () =>
            {
                start.Wait();
                return await new Sender(provider, cache).SendAsync(new SampleCommand(index), CancellationToken.None);
            }))
            .ToList();
        start.Set();
        var results = await Task.WhenAll(sends);

        results.Should().HaveCount(ConcurrentSends).And.OnlyContain(result => result.IsSuccess);
        cache.Count.Should().Be(1);
        await handler.Received(ConcurrentSends).Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SameCommandTypeTwice_ResolvesHandlerFromProviderEachTimeByResolveCount()
    {
        var handler = Substitute.For<ICommandHandler<SampleCommand, Unit>>();
        handler.Handle(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var provider = new ServiceProviderStub().Add(handler);
        var sender = new Sender(provider, new RequestInvokerCache());

        await sender.SendAsync(new SampleCommand(1), CancellationToken.None);
        await sender.SendAsync(new SampleCommand(2), CancellationToken.None);

        // 캐시는 형식 정보(호출기)만 담고 Handler 인스턴스는 매번 현재 스코프의 IServiceProvider에서 꺼낸다(ADR-0015).
        provider.ResolveCount<ICommandHandler<SampleCommand, Unit>>().Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_TwoSendersWithDifferentProvidersSharingCache_EachUsesOwnProvidersHandler()
    {
        var cache = new RequestInvokerCache();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var firstHandler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        firstHandler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(firstId));
        var secondHandler = Substitute.For<ICommandHandler<CreateSampleCommand, Guid>>();
        secondHandler.Handle(Arg.Any<CreateSampleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(secondId));

        var first = await new Sender(new ServiceProviderStub().Add(firstHandler), cache)
            .SendAsync(new CreateSampleCommand("a"), CancellationToken.None);
        var second = await new Sender(new ServiceProviderStub().Add(secondHandler), cache)
            .SendAsync(new CreateSampleCommand("b"), CancellationToken.None);

        first.Value.Should().Be(firstId);
        second.Value.Should().Be(secondId);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WithPublicConstructor_RegistersInvokerInSharedCacheByContains()
    {
        var handler = Substitute.For<ICommandHandler<SharedCacheProbeCommand, Unit>>();
        handler.Handle(Arg.Any<SharedCacheProbeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));
        var sender = new Sender(new ServiceProviderStub().Add(handler));

        await sender.SendAsync(new SharedCacheProbeCommand(), CancellationToken.None);

        RequestInvokerCache.Shared.ContainsCommand(typeof(SharedCacheProbeCommand), typeof(Unit)).Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_CommandImplementingTwoResponseTypes_DispatchesByCallSiteResponseTypeWithoutCastFailure()
    {
        var cache = new RequestInvokerCache();
        var intHandler = Substitute.For<ICommandHandler<DualResponseCommand, int>>();
        intHandler.Handle(Arg.Any<DualResponseCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(42));
        var stringHandler = Substitute.For<ICommandHandler<DualResponseCommand, string>>();
        stringHandler.Handle(Arg.Any<DualResponseCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success("응답"));
        var sender = new Sender(new ServiceProviderStub().Add(intHandler).Add(stringHandler), cache);
        var command = new DualResponseCommand();

        var asInt = await sender.SendAsync<int>(command, CancellationToken.None);
        var asString = await sender.SendAsync<string>(command, CancellationToken.None);

        asInt.Value.Should().Be(42);
        asString.Value.Should().Be("응답");
        cache.Count.Should().Be(2);
    }

    [Fact]
    public async Task SendAsyncAndQueryAsync_SameRequestTypeAsCommandAndQuery_UseSeparateHandlersAndCacheEntries()
    {
        var cache = new RequestInvokerCache();
        var commandHandler = Substitute.For<ICommandHandler<CommandAndQueryRequest, int>>();
        commandHandler.Handle(Arg.Any<CommandAndQueryRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success(1));
        var queryHandler = Substitute.For<IQueryHandler<CommandAndQueryRequest, int>>();
        queryHandler.Handle(Arg.Any<CommandAndQueryRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success(2));
        var sender = new Sender(new ServiceProviderStub().Add(commandHandler).Add(queryHandler), cache);
        var request = new CommandAndQueryRequest();

        var commandResult = await sender.SendAsync<int>(request, CancellationToken.None);
        var queryResult = await sender.QueryAsync<int>(request, CancellationToken.None);

        commandResult.Value.Should().Be(1);
        queryResult.Value.Should().Be(2);
        cache.Count.Should().Be(2);
    }

    /// <summary>공유 캐시 확인 전용 Command. 다른 테스트가 쓰지 않는 형식이어야 한다.</summary>
    public sealed record SharedCacheProbeCommand : ICommand;
}
