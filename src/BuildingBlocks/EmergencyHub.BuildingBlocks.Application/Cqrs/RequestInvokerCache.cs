using System.Collections.Concurrent;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 요청 형식별 호출기(<see cref="CommandInvoker{TResponse}"/> / <see cref="QueryInvoker{TResponse}"/>) 캐시입니다(ADR-0015 "Handler 타입 캐시").
/// </summary>
/// <remarks>
/// <para>
/// 키는 (요청의 런타임 형식, 응답 형식)입니다. 응답 형식을 키에 넣어 한 형식이 <c>ICommand&lt;A&gt;</c>와 <c>ICommand&lt;B&gt;</c>를
/// 함께 구현해도 형 변환 오류가 나지 않고, Command와 Query는 서로 다른 사전을 써서 섞이지 않습니다.
/// </para>
/// <para>
/// 값은 <see cref="Lazy{T}"/>(기본 <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>)라 동시에 처음 요청해도 호출기를 한 번만 만듭니다.
/// 호출기는 형식 정보만 담고 Handler 인스턴스를 담지 않으므로 스레드 · 스코프와 무관하게 공유합니다.
/// Handler 등록 누락은 호출 시점에 확인하므로 캐시에 남지 않습니다.
/// </para>
/// </remarks>
internal sealed class RequestInvokerCache
{
    private readonly ConcurrentDictionary<(Type Request, Type Response), Lazy<object>> _commandInvokers = new();
    private readonly ConcurrentDictionary<(Type Request, Type Response), Lazy<object>> _queryInvokers = new();

    /// <summary>프로세스 전체가 공유하는 캐시입니다. <see cref="Sender"/>의 public 생성자가 씁니다.</summary>
    public static RequestInvokerCache Shared { get; } = new();

    /// <summary>캐시 항목 수(Command + Query)입니다.</summary>
    public int Count => _commandInvokers.Count + _queryInvokers.Count;

    /// <summary>Command 호출기 항목이 있는지 확인합니다.</summary>
    public bool ContainsCommand(Type commandType, Type responseType) => _commandInvokers.ContainsKey((commandType, responseType));

    /// <summary>Query 호출기 항목이 있는지 확인합니다.</summary>
    public bool ContainsQuery(Type queryType, Type responseType) => _queryInvokers.ContainsKey((queryType, responseType));

    /// <summary>Command 형식의 호출기를 돌려줍니다. 없으면 한 번 만들어 캐시합니다.</summary>
    /// <typeparam name="TResponse">성공 값 형식.</typeparam>
    /// <param name="commandType">요청의 런타임 형식. <c>ICommand&lt;TResponse&gt;</c>를 구현해야 합니다.</param>
    /// <returns>캐시된 호출기.</returns>
    public CommandInvoker<TResponse> GetCommandInvoker<TResponse>(Type commandType)
    {
        EnsureImplements(commandType, typeof(ICommand<TResponse>), nameof(commandType));

        var invoker = _commandInvokers.GetOrAdd(
            (commandType, typeof(TResponse)),
            static key => new Lazy<object>(() => Create(typeof(CommandInvoker<,>), key)));
        return (CommandInvoker<TResponse>)invoker.Value;
    }

    /// <summary>Query 형식의 호출기를 돌려줍니다. 없으면 한 번 만들어 캐시합니다.</summary>
    /// <typeparam name="TResponse">응답 형식.</typeparam>
    /// <param name="queryType">요청의 런타임 형식. <c>IQuery&lt;TResponse&gt;</c>를 구현해야 합니다.</param>
    /// <returns>캐시된 호출기.</returns>
    public QueryInvoker<TResponse> GetQueryInvoker<TResponse>(Type queryType)
    {
        EnsureImplements(queryType, typeof(IQuery<TResponse>), nameof(queryType));

        var invoker = _queryInvokers.GetOrAdd(
            (queryType, typeof(TResponse)),
            static key => new Lazy<object>(() => Create(typeof(QueryInvoker<,>), key)));
        return (QueryInvoker<TResponse>)invoker.Value;
    }

    // 검증을 GetOrAdd 앞에 둔다. Lazy는 생성 예외도 캐시하므로 잘못된 형식이 항목으로 남지 않게 한다.
    private static void EnsureImplements(Type requestType, Type requestInterface, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(requestType, parameterName);

        if (!requestInterface.IsAssignableFrom(requestType))
        {
            throw new ArgumentException($"형식 '{requestType.FullName}'은 {requestInterface}를 구현하지 않습니다.", parameterName);
        }
    }

    private static object Create(Type openInvokerType, (Type Request, Type Response) key) =>
        Activator.CreateInstance(openInvokerType.MakeGenericType(key.Request, key.Response))!;
}
