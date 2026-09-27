using EmergencyHub.BuildingBlocks.Application.Identifiers;
using UUIDNext;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;

/// <summary>
/// UUIDNext로 PostgreSQL 정렬에 맞는 UUID v7을 만드는 <see cref="IIdGenerator"/> 구현입니다(ADR-0013).
/// </summary>
/// <remarks>
/// 단조성 상태(12비트 카운터 · lock)는 UUIDNext의 정적 생성기에 있으므로, Scoped로 인스턴스가 여러 개 생겨도
/// 프로세스 안 생성 순서는 이어집니다. 시계는 <c>TimeProvider</c>가 아니라 <c>DateTimeOffset.UtcNow</c>라서 단위 테스트는 이 형식 대신 대역을 씁니다.
/// </remarks>
internal sealed class UuidV7IdGenerator : IIdGenerator
{
    public Guid NewId() => Uuid.NewDatabaseFriendly(Database.PostgreSql);
}
