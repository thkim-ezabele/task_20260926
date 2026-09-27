using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// PostgresException public 생성자로 규칙표(database.md "영속성 예외 변환") 입력을 만든다.
/// Detail · MessageText에 값(개인정보 가정)을 넣어 Result 메시지 · 로그에 새지 않는지 확인한다.
/// </remarks>
public static class SamplePostgresExceptions
{
    public const string SecretValue = "secret.person@example.com";

    public const string SecretDetail = $"Key (order_number)=({SecretValue}) already exists.";

    public const string SecretMessageText = $"duplicate key value {SecretValue} violates unique constraint";

    public static PostgresException Create(string sqlState, string? constraintName = null) =>
        new(
            messageText: SecretMessageText,
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: sqlState,
            detail: SecretDetail,
            constraintName: constraintName);

    public static DbUpdateException Wrap(Exception? inner, params EntityEntry[] entries) =>
        new("An error occurred while saving the entity changes.", inner, entries);

    public static DbUpdateException UniqueViolation(string? constraintName, params EntityEntry[] entries) =>
        Wrap(Create(PostgresErrorCodes.UniqueViolation, constraintName), entries);

    public static DbUpdateConcurrencyException Concurrency(params EntityEntry[] entries) =>
        new("The database operation was expected to affect 1 row(s), but actually affected 0 row(s).", null, UpdateEntries(entries));

    [SuppressMessage(
        "Usage",
        "EF1001:Internal EF Core API usage.",
        Justification = "DbUpdateConcurrencyException에는 IUpdateEntry 목록 생성자만 있어, EF가 던지는 것과 같은 모양(Entries 포함)을 만들려면 내부 엔트리가 필요하다(테스트 전용).")]
    private static List<IUpdateEntry> UpdateEntries(EntityEntry[] entries) =>
        [.. entries.Select(entry => (IUpdateEntry)entry.GetInfrastructure())];
}
