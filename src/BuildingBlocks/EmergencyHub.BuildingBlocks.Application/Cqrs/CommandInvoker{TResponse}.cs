using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 응답 형식만 아는 호출 지점(<see cref="Sender.SendAsync{TResponse}"/>)에서 런타임 Command 형식의 Handler를 부르기 위한 기반 호출기입니다.
/// </summary>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
internal abstract class CommandInvoker<TResponse>
{
    /// <summary>현재 스코프에서 Handler를 꺼내 Command를 처리합니다.</summary>
    /// <param name="command">처리할 Command.</param>
    /// <param name="serviceProvider">현재 스코프의 서비스 공급자.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>Handler의 결과.</returns>
    public abstract Task<Result<TResponse>> InvokeAsync(
        ICommand<TResponse> command,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
