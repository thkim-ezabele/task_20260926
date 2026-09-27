using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Command / Query를 해당 Handler(데코레이터 파이프라인 포함)로 보내는 디스패처입니다(ADR-0015).
/// Controller는 이 인터페이스만 주입받습니다(ADR-0016).
/// </summary>
/// <remarks>
/// <para>
/// Scoped로 등록하며 현재 스코프의 <see cref="IServiceProvider"/>로 Handler를 해석합니다.
/// Handler가 등록되어 있지 않으면 요청 형식 이름을 담은 <see cref="InvalidOperationException"/>을 던집니다(프로그래밍 오류라 <see cref="Result"/>가 아님).
/// </para>
/// <para>
/// Handler 안에서 호출하지 않습니다(중첩 Send 금지, Command 하나 = 트랜잭션 하나).
/// 반환 값이 없는 <see cref="ICommand"/>도 <c>TResponse = Unit</c>으로 추론되어 <see cref="SendAsync{TResponse}"/>로 보냅니다.
/// </para>
/// </remarks>
public interface ISender
{
    /// <summary>Command를 Handler로 보냅니다.</summary>
    /// <typeparam name="TResponse">성공 값 형식. <see cref="ICommand"/>면 <see cref="Unit"/>입니다.</typeparam>
    /// <param name="command">보낼 Command.</param>
    /// <param name="cancellationToken">취소 토큰. Handler까지 그대로 전달합니다.</param>
    /// <returns>Handler(파이프라인)의 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">Command의 Handler가 등록되어 있지 않은 경우.</exception>
    Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken);

    /// <summary>Query를 Handler로 보냅니다.</summary>
    /// <typeparam name="TResponse">응답 형식.</typeparam>
    /// <param name="query">보낼 Query.</param>
    /// <param name="cancellationToken">취소 토큰. Handler까지 그대로 전달합니다.</param>
    /// <returns>Handler(파이프라인)의 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="InvalidOperationException">Query의 Handler가 등록되어 있지 않은 경우.</exception>
    Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken);
}
