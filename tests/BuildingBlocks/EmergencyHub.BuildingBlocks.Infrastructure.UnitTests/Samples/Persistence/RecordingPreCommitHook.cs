using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>Outbox 확장 지점 대역. 호출될 때 단계와 그 시점의 DbContext · 엔트리 상태를 기록한다.</remarks>
public sealed class RecordingPreCommitHook(FakeDatabase database) : IPreCommitHook
{
    private readonly List<WriteDbContextBase> _contexts = [];
    private readonly List<EntityState[]> _states = [];

    public IReadOnlyList<WriteDbContextBase> Contexts => _contexts;

    public IReadOnlyList<EntityState[]> EntryStates => _states;

    public Task BeforeCommitAsync(WriteDbContextBase context, CancellationToken cancellationToken)
    {
        database.Record("Hook");
        _contexts.Add(context);
        _states.Add([.. context.ChangeTracker.Entries().Select(entry => entry.State)]);
        return Task.CompletedTask;
    }
}
