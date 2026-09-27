using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// <see cref="ISender"/>의 직접 구현입니다(ADR-0015).
/// 요청의 런타임 형식마다 한 번 만든 강타입 호출기를 <see cref="RequestInvokerCache"/>에 두고, 이후 호출은 리플렉션 없이 호출기로 보냅니다.
/// </summary>
/// <remarks>
/// 캐시는 형식 정보만 담으므로 스레드 · 스코프와 무관하게 공유하고, Handler 인스턴스는 매 호출 주입된 <see cref="IServiceProvider"/>(현재 스코프)에서 꺼냅니다.
/// </remarks>
internal sealed class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RequestInvokerCache _cache;

    // DI 컨테이너는 public 생성자만 고르므로 이 생성자만 쓰고 공유 캐시를 쓴다.
    public Sender(IServiceProvider serviceProvider)
        : this(serviceProvider, RequestInvokerCache.Shared)
    {
    }

    // 테스트가 격리된 캐시로 항목 수를 판정하기 위한 생성자.
    internal Sender(IServiceProvider serviceProvider, RequestInvokerCache cache)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(cache);

        _serviceProvider = serviceProvider;
        _cache = cache;
    }

    public async Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoker = _cache.GetCommandInvoker<TResponse>(command.GetType());
        return await invoker.InvokeAsync(command, _serviceProvider, cancellationToken);
    }

    public async Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoker = _cache.GetQueryInvoker<TResponse>(query.GetType());
        return await invoker.InvokeAsync(query, _serviceProvider, cancellationToken);
    }
}
