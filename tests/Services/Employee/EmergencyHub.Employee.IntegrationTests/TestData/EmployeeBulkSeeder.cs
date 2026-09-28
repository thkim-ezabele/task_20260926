using EmergencyHub.Employee.IntegrationTests.Fixtures;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 조회 성능 · 계획 확인용 10,000건 fixture 시더입니다(PRD-002 NFR-03, S07-T03). 통합 테스트 전용이며 마이그레이션 시드(<c>HasData</c>)가 아닙니다(database.md "시드 데이터").
/// </summary>
/// <remarks>
/// <para>삽입: 쓰기 연결에서 <c>INSERT ... SELECT generate_series</c> 한 문장(서버 안에서 행 생성, 수십 ms)으로 넣고 바로 <c>ANALYZE employees</c>를 실행합니다.
/// EF <c>AddRange</c>는 10,000개 Aggregate를 만들고 배치로 보내야 해 느리고, 조회 측정과 무관한 쓰기 경로를 섞으므로 쓰지 않습니다.
/// <c>VACUUM</c>은 여러 문장 명령(암묵 트랜잭션) 안에서 실행할 수 없어 하지 않습니다(가시성 맵 없음 → Index Only Scan 대신 Bitmap Index Scan이 될 수 있음).</para>
/// <para>Respawn과의 관계: <see cref="EmployeeDatabaseTest"/>가 테스트 시작 전에 비운 뒤 이 시더를 부릅니다. 비운 뒤 통계는 낡으므로 시더가 매번 ANALYZE합니다.
/// 같은 테스트 안에서 <see cref="EmployeeDatabaseFixture.ResetAsync"/>를 다시 부르면 시드도 지워집니다(회차마다 다시 시드하지 말고 한 번 시드한 뒤 조회만 반복).</para>
/// <para>행 규칙(결정적, 난수 없음, g = 1 ~ 10,000):</para>
/// <list type="bullet">
/// <item><c>id</c>: <c>0190a000-0000-7000-8000-{g 16진수 12자리}</c>(버전 7 · 변형 8 형태, g 순으로 증가).</item>
/// <item><c>name</c>: <c>직원{g % 2000}</c>(한글, 이름 2,000종 × 5명 = 동명이인). <see cref="NamesakeName"/>은 g = 42 · 2042 · 4042 · 6042 · 8042.</item>
/// <item><c>joined_on</c>: <c>2015-01-01 + (g × 37) % 3650</c>일. 하루에 2~3명이라 같은 입사일에서 <c>id</c> 순 보조 정렬이 쓰이고, 목록 순서가 ID 순서와 다릅니다.
/// 동명이인 <see cref="NamesakeName"/> 중 가장 빠른 사람은 ID가 가장 작은 사람이 아닙니다(g = 6042, 입사일 2017-06-23).</item>
/// <item><c>email</c>: <c>Seed{g}@Example.com</c>, <c>normalized_email</c>은 소문자(DB는 강제하지 않음, ADR-0027).</item>
/// <item><c>employee_status</c>: g % 10 = 0이면 2(비활성), 아니면 1. 목록 · 개수 · 이름 조회는 상태와 무관합니다.</item>
/// </list>
/// </remarks>
public static class EmployeeBulkSeeder
{
    /// <summary>시드 행 수입니다.</summary>
    public const int Rows = 10_000;

    /// <summary>이름 종류 수입니다(이름당 <see cref="Rows"/> / <see cref="DistinctNames"/> = 5명).</summary>
    public const int DistinctNames = 2_000;

    /// <summary>이름 조회 대상(동명이인 5명)입니다.</summary>
    public const string NamesakeName = "직원42";

    /// <summary>시드에 없는 이름입니다(404 확인용).</summary>
    public const string MissingName = "직원없음";

    private const string SeedSql =
        """
        INSERT INTO employees (id, name, email, normalized_email, phone_number, joined_on, employee_status, created_at, updated_at)
        SELECT ('0190a000-0000-7000-8000-' || lpad(to_hex(g), 12, '0'))::uuid,
               '직원' || (g % 2000),
               'Seed' || g || '@Example.com',
               'seed' || g || '@example.com',
               '010-' || lpad((g % 10000)::text, 4, '0') || '-' || lpad((g % 7919)::text, 4, '0'),
               date '2015-01-01' + (g * 37) % 3650,
               CASE WHEN g % 10 = 0 THEN 2 ELSE 1 END,
               now(), now()
        FROM generate_series(1, 10000) AS g;
        ANALYZE employees;
        """;

    /// <summary>10,000건을 넣고 통계를 갱신합니다. 테이블이 비어 있어야 합니다(테스트 시작 전 Respawn).</summary>
    /// <param name="database">컬렉션 fixture.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>작업.</returns>
    /// <exception cref="InvalidOperationException">넣은 뒤 행 수가 <see cref="Rows"/>가 아닌 경우(비우지 않고 부른 경우 포함).</exception>
    public static async Task SeedAsync(EmployeeDatabaseFixture database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await connection.ExecuteSqlAsync(SeedSql, cancellationToken);
        var count = await connection.ScalarAsync<long>("SELECT count(*) FROM employees", cancellationToken);
        if (count != Rows)
        {
            throw new InvalidOperationException($"시드 뒤 행 수가 {Rows}이 아니라 {count}입니다. 테스트 시작 전 Respawn으로 비웠는지 확인하세요.");
        }
    }

    /// <summary>목록 순서(<c>joined_on</c> → <c>id</c>)로 한 쪽의 ID를 SQL로 읽습니다(HTTP 응답 대조용 기대값).</summary>
    /// <param name="database">컬렉션 fixture.</param>
    /// <param name="skip">건너뛸 행 수.</param>
    /// <param name="take">가져올 행 수.</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>ID 목록.</returns>
    public static async Task<IReadOnlyList<Guid>> IdsInListOrderAsync(EmployeeDatabaseFixture database, int skip, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM employees ORDER BY joined_on, id OFFSET $1 LIMIT $2", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = skip });
        command.Parameters.Add(new NpgsqlParameter { Value = take });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    /// <summary>이름이 같은 직원 중 목록 순서로 첫 사람의 ID를 SQL로 읽습니다(이름 조회 기대값). 없으면 <see langword="null"/>입니다.</summary>
    /// <param name="database">컬렉션 fixture.</param>
    /// <param name="name">이름(저장 값 그대로, NFC).</param>
    /// <param name="cancellationToken">취소 토큰.</param>
    /// <returns>ID.</returns>
    public static async Task<Guid?> FirstIdByNameAsync(EmployeeDatabaseFixture database, string name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        await using var connection = await database.OpenWriteConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT id FROM employees WHERE name = $1 ORDER BY joined_on, id LIMIT 1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = name });
        return await command.ExecuteScalarAsync(cancellationToken) as Guid?;
    }
}
