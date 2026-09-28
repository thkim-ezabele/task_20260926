using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// 스코프마다 하나씩 만들어져 자기 커밋 횟수를 세는 <see cref="IUnitOfWork"/> 구현입니다(동시 Send 인수 테스트용).
/// </summary>
/// <remarks>마커를 상속하지 않으므로 자동 등록되지 않고, 테스트가 Scoped 팩토리로 등록합니다.</remarks>
public sealed class CountingUnitOfWork : IUnitOfWork
{
    private int _commits;

    /// <summary>이 인스턴스에서 <see cref="CommitAsync"/>가 불린 횟수.</summary>
    public int Commits => Volatile.Read(ref _commits);

    /// <inheritdoc />
    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        // 다른 스코프의 요청과 실제로 겹치도록 제어를 한 번 양보한다.
        await Task.Yield();
        Interlocked.Increment(ref _commits);
        return Result.Success();
    }
}
