using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>감사 인터셉터 뒤에 두어 실제 저장(DB 호출)을 건너뛴다. 인터셉터 경로를 DB 없이 확인하기 위한 대역.</remarks>
public sealed class SuppressSaveInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        InterceptionResult<int>.SuppressWithResult(0);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
}
