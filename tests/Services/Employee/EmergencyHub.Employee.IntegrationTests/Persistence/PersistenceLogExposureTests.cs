using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// BL-081 P8 · BL-023 보강(testing-strategy.md "장애 주입" P8): EfCoreErrorLogLevelTests가 수준(Error 0, Debug)을 고정하므로 여기서는
// 23505 한 건당 EF 실패 이벤트 건수와, 모든 범주(EF · UnitOfWork · Npgsql)의 메시지 · 구조화 속성 · 예외 문자열에 이메일 · 서버 DETAIL이 없는지만 본다.
// 연결에 Include Error Detail이 없으므로 PostgresException.Detail은 가려진다(연결 문자열 금지 옵션, database.md "영속성 예외 변환").
// S05-T04: 유니크 인덱스가 normalized_email로 바뀌어 DETAIL 조각과 입력 이메일(대소문자 섞음) · 정규화 이메일 · 전화번호를 함께 본다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-001/FR-05")]
public sealed class PersistenceLogExposureTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // 서버 23505 DETAIL 형식: Key (normalized_email)=(...) already exists.
    private const string UniqueViolationDetailFragment = "Key (normalized_email)";

    // ---- 성공 ----

    [Fact]
    public async Task CommitAsync_OneDuplicateEmail_LogsExactlyOneCommandErrorAndOneSaveChangesFailed()
    {
        await using var services = Database.CreateServices();
        var email = $"count-{Guid.NewGuid():N}@example.com";
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        services.GetFakeLogCollector().Clear();

        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken)).IsFailure.Should().BeTrue();

        var logs = services.GetFakeLogCollector().GetSnapshot();
        logs.Count(record => record.Id.Id == RelationalEventId.CommandError.Id).Should().Be(1);
        logs.Count(record => record.Id.Id == CoreEventId.SaveChangesFailed.Id).Should().Be(1);
        logs.Should().NotContain(record => record.Id.Id == RelationalEventId.TransactionError.Id, "23505는 SaveChanges에서 나며 커밋 오류가 아니다");
    }

    // ---- 실패 ----

    [Fact]
    public async Task CommitAsync_DuplicateEmail_NoLogRecordOrResultExposesEmailOrServerDetail()
    {
        await using var services = Database.CreateServices();
        var email = $"Secret-{Guid.NewGuid():N}@Example.com";
        const string PhoneNumber = "010-9876-5432";
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithEmail(email).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        services.GetFakeLogCollector().Clear();

        var result = await EmployeeCommits.AddAndCommitAsync(
            services, new EmployeeBuilder().WithEmail(email.ToUpperInvariant()).WithPhoneNumber(PhoneNumber).Build(), CancellationToken);

        result.Error.Message.Should().NotContain(email);
        var texts = services.GetFakeLogCollector().GetSnapshot().Select(Flatten).ToList();
        texts.Should().NotBeEmpty();
        texts.Should().NotContain(text => text.Contains(email, StringComparison.OrdinalIgnoreCase), "입력 표기 · 정규화 이메일 모두 없어야 한다");
        texts.Should().NotContain(text => text.Contains(PhoneNumber, StringComparison.Ordinal));
        texts.Should().NotContain(text => text.Contains(UniqueViolationDetailFragment, StringComparison.Ordinal));
    }

    private static string Flatten(FakeLogRecord record) =>
        string.Join(
            '\n',
            record.Message,
            record.StructuredState is null ? string.Empty : string.Join('|', record.StructuredState.Select(pair => $"{pair.Key}={pair.Value}")),
            record.Exception?.ToString() ?? string.Empty);
}
