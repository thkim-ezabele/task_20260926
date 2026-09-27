using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Command 전용 트랜잭션 데코레이터입니다. Handler 바로 바깥에서 실행합니다(ADR-0014, ADR-0015).
/// </summary>
/// <typeparam name="TCommand">Command 형식.</typeparam>
/// <typeparam name="TResponse">성공 값 형식.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>Handler를 정확히 한 번 실행합니다(재시도 루프 없음. 재시도는 UoW 안의 실행 전략 몫).</description></item>
/// <item><description>Handler가 실패 결과면 <see cref="IUnitOfWork.CommitAsync"/>를 부르지 않고 같은 결과를 돌려줍니다.</description></item>
/// <item><description>성공일 때만 <see cref="IUnitOfWork.CommitAsync"/>를 받은 취소 토큰으로 한 번 부릅니다.</description></item>
/// <item><description>커밋이 성공하면 Handler의 결과를 그대로, 실패하면 커밋의 <c>Error</c>를 바꾸지 않고 실패 결과로 돌려줍니다(롤백은 UoW가 이미 끝냄).</description></item>
/// <item><description>Handler · 커밋 예외는 잡지 않습니다. DbContext · 트랜잭션 · 실행 전략 · <c>SaveChanges</c>는 다루지 않습니다(모두 UoW 안).</description></item>
/// </list>
/// <see cref="IQueryHandler{TQuery, TResponse}"/>는 감싸지 않습니다(Query는 트랜잭션 없음).
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddBuildingBlocksApplication(S02-T03)이 open generic 데코레이터로 등록해 DI가 만든다(ADR-0015).")]
internal sealed class TransactionCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IUnitOfWork unitOfWork) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var result = await inner.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        var commit = await unitOfWork.CommitAsync(cancellationToken);
        return commit.IsSuccess ? result : Result.Failure<TResponse>(commit.Error);
    }
}
