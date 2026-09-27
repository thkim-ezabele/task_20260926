using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Auditing;

/// <summary>
/// 저장 직전에 감사 shadow property(<c>created_at</c> · <c>updated_at</c>)를 채우는 인터셉터입니다(database.md "감사 컬럼").
/// </summary>
/// <remarks>
/// <para>
/// 시각은 <see cref="TimeProvider.GetUtcNow"/>이고 저장 전에 오프셋 0으로 정규화합니다. 로컬 시간대가 +09:00이거나 공급자 구현이 오프셋을 붙여 돌려줘도
/// UTC로 저장합니다(Npgsql은 오프셋이 0이 아닌 값을 timestamptz에 쓰지 않음).
/// </para>
/// <para>실행 전략이 재시도하면 다시 실행되어 시각이 다시 계산됩니다. 이것은 허용합니다(S02 계획 리뷰, ADR-0014). 쓰기 DbContext에만 붙입니다.</para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "공통 DbContext 등록 확장(S02-T07)이 쓰기 DbContext에 붙인다. 등록 코드가 생기면 이 억제를 지운다.")]
internal sealed class AuditSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 변경 추적 상태에 따라 감사 값을 채웁니다. 인터셉터 두 경로(동기 · 비동기)가 이 메서드를 부르며, DB 없이 단위 테스트할 수 있습니다.
    /// </summary>
    /// <param name="changeTracker">변경 추적기.</param>
    /// <param name="now">저장 시각. 오프셋이 0이 아니면 같은 순간의 UTC 값으로 바꿔 씁니다.</param>
    /// <remarks>
    /// Added: 둘 다 now. Modified: <c>updated_at</c>만 now, <c>created_at</c>은 UPDATE 대상에서 뺌.
    /// owned 엔트리가 Added / Modified / Deleted면 owned가 아닌 최상위 소유자의 <c>updated_at</c> = now(소유자가 Unchanged면 Modified가 되어 xmin 검사도 걸림).
    /// Deleted · Unchanged는 그대로 둡니다.
    /// </remarks>
    internal static void ApplyAuditValues(ChangeTracker changeTracker, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(changeTracker);
        var utcNow = now.ToUniversalTime();

        // 인터셉터 시점에는 자동 변경 감지 전일 수 있다(AutoDetectChangesEnabled = false 포함).
        changeTracker.DetectChanges();
        var entries = changeTracker.Entries().ToList();

        var touchedOwners = entries
            .Where(entry => entry.Metadata.IsOwned() && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => FindRootOwner(entries, entry))
            .OfType<EntityEntry>()
            .Select(owner => owner.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);

        foreach (var entry in entries.Where(entry => !entry.Metadata.IsOwned() && HasAuditProperties(entry)))
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(ShadowPropertyNames.CreatedAt).CurrentValue = utcNow;
                    entry.Property(ShadowPropertyNames.UpdatedAt).CurrentValue = utcNow;
                    break;
                case EntityState.Modified:
                case EntityState.Unchanged when touchedOwners.Contains(entry.Entity):
                    MarkUpdated(entry, utcNow);
                    break;
            }
        }
    }

    private static void MarkUpdated(EntityEntry entry, DateTimeOffset now)
    {
        var updatedAt = entry.Property(ShadowPropertyNames.UpdatedAt);
        updatedAt.CurrentValue = now;

        // 값이 이미 now와 같아도 UPDATE(→ 소유자 xmin 검사)가 일어나야 한다.
        updatedAt.IsModified = true;
        entry.Property(ShadowPropertyNames.CreatedAt).IsModified = false;
    }

    private static bool HasAuditProperties(EntityEntry entry) =>
        entry.Metadata.FindProperty(ShadowPropertyNames.CreatedAt) is not null
        && entry.Metadata.FindProperty(ShadowPropertyNames.UpdatedAt) is not null;

    // owned 엔트리에서 소유 관계의 외래 키 값으로 소유자 엔트리를 찾아, owned가 아닌 엔트리가 나올 때까지(중첩 owned 포함) 올라간다.
    // 삭제된 owned는 소유자 탐색 속성에서 이미 빠졌으므로 소유자 쪽에서 내려가지 않고 owned 쪽에서 올라간다.
    private static EntityEntry? FindRootOwner(IReadOnlyList<EntityEntry> entries, EntityEntry ownedEntry)
    {
        var current = ownedEntry;
        while (current.Metadata.FindOwnership() is { } ownership)
        {
            var child = current;
            var foreignKeyValues = ownership.Properties.Select(property => child.Property(property.Name).CurrentValue).ToList();
            var owner = entries.FirstOrDefault(candidate =>
                ownership.PrincipalEntityType.IsAssignableFrom(candidate.Metadata)
                && ownership.PrincipalKey.Properties.Select(property => candidate.Property(property.Name).CurrentValue).SequenceEqual(foreignKeyValues));

            if (owner is null)
            {
                return null;
            }

            current = owner;
        }

        return current;
    }

    private void Stamp(DbContextEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is { } context)
        {
            ApplyAuditValues(context.ChangeTracker, _timeProvider.GetUtcNow());
        }
    }
}
