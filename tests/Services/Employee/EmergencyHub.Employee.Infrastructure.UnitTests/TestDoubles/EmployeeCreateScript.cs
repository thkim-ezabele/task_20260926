namespace EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

/// <remarks>
/// 생성 SQL(<c>GenerateCreateScript</c> · idempotent 마이그레이션 스크립트)에서 <c>employees</c> 표를 대조하는 도우미다(S05-T04 dba 명세).
/// <c>ITable.Columns</c>는 선언 순서를 보장하지 않으므로 열 순서는 <c>CREATE TABLE employees (</c> 블록의 줄로 확인한다.
/// idempotent 스크립트는 <c>DO $EF$</c> 블록 안이라 들여쓰기가 다르므로 줄마다 Trim해 비교한다.
/// </remarks>
internal static class EmployeeCreateScript
{
    public const string CreateTableHeader = "CREATE TABLE employees (";

    public const string UniqueIndexSql = "CREATE UNIQUE INDEX ux_employees_normalized_email ON employees (normalized_email);";

    public const string JoinedOnIdIndexSql = "CREATE INDEX ix_employees_joined_on_id ON employees (joined_on, id);";

    public const string NameJoinedOnIdIndexSql = "CREATE INDEX ix_employees_name_joined_on_id ON employees (name, joined_on, id);";

    /// <summary>database.md "새 스키마 명세" 컬럼 표 순서 + 제약 2줄. `email ...` 줄은 `normalized_email ...` 줄의 부분 문자열이라 줄 단위로 같아야 한다.</summary>
    public static readonly string[] ExpectedTableLines =
    [
        "id uuid NOT NULL,",
        "name character varying(100) NOT NULL,",
        "email character varying(254) NOT NULL,",
        "normalized_email character varying(254) NOT NULL,",
        "phone_number character varying(20) NOT NULL,",
        "joined_on date NOT NULL,",
        "employee_status smallint NOT NULL,",
        "created_at timestamp with time zone NOT NULL,",
        "updated_at timestamp with time zone NOT NULL,",
        "CONSTRAINT pk_employees PRIMARY KEY (id),",
        "CONSTRAINT ck_employees_employee_status CHECK (employee_status IN (1, 2))",
    ];

    /// <summary><c>CREATE TABLE employees (</c> 다음 줄부터 <c>);</c> 앞 줄까지를 Trim해 돌려준다. 머리 줄이 없으면 빈 목록이다.</summary>
    public static IReadOnlyList<string> TableLines(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var lines = script.Split('\n').Select(line => line.Trim()).ToList();
        var header = lines.IndexOf(CreateTableHeader);
        if (header < 0)
        {
            return [];
        }

        return lines.Skip(header + 1).TakeWhile(line => line != ");").ToList();
    }

    /// <summary><paramref name="text"/>가 <paramref name="script"/>에 몇 번 나오는지 센다(겹치지 않게, Ordinal).</summary>
    public static int Count(string script, string text)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentException.ThrowIfNullOrEmpty(text);

        var count = 0;
        for (var at = script.IndexOf(text, StringComparison.Ordinal); at >= 0; at = script.IndexOf(text, at + text.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
