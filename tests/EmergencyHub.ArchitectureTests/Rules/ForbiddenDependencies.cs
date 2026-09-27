namespace EmergencyHub.ArchitectureTests.Rules;

/// <summary>
/// 의존 금지 대상의 이름 접두사(네임스페이스 = 패키지 이름 접두사). 형식 의존 규칙과 선언 참조 규칙이 함께 쓴다.
/// </summary>
/// <remarks>
/// 접두사는 점 단위로 비교한다(<c>Npgsql</c>은 <c>Npgsql.EntityFrameworkCore.PostgreSQL</c>과 맞고 <c>NpgsqlX</c>와는 맞지 않음).
/// 원본: ADR-0024 의존성 규칙 표 "참조 금지" 열.
/// </remarks>
public static class ForbiddenDependencies
{
    /// <summary>EF Core(<c>Microsoft.EntityFrameworkCore.*</c>)와 EF Core 확장(<c>EFCore.NamingConventions</c>).</summary>
    public static IReadOnlyList<string> EfCore { get; } = ["Microsoft.EntityFrameworkCore", "EFCore"];

    /// <summary>Npgsql(ADO.NET 공급자 · EF Core 공급자).</summary>
    public static IReadOnlyList<string> Npgsql { get; } = ["Npgsql"];

    /// <summary>Scrutor(타입 검색, BuildingBlocks.Infrastructure 전용, ADR-0017).</summary>
    public static IReadOnlyList<string> Scrutor { get; } = ["Scrutor"];

    /// <summary>
    /// ASP.NET Core와 웹 API 문서화(Swashbuckle · Microsoft.OpenApi). ADR-0024가 막는 것은 웹 API 규약 코드와 Swashbuckle이
    /// Infrastructure 쪽으로 번지는 것이다.
    /// </summary>
    public static IReadOnlyList<string> AspNetCore { get; } = ["Microsoft.AspNetCore", "Swashbuckle", "Microsoft.OpenApi"];

    /// <summary>직렬화 라이브러리(Domain 금지, ADR-0024 BuildingBlocks.Domain 행). BCL 안에 있어 <c>System</c> 허용과 따로 막는다.</summary>
    public static IReadOnlyList<string> Serialization { get; } = ["System.Text.Json", "System.Runtime.Serialization", "System.Xml.Serialization"];

    /// <summary>여러 금지 목록을 하나로 합친다.</summary>
    /// <param name="groups">금지 목록.</param>
    /// <returns>합친 목록.</returns>
    public static string[] Combine(params IReadOnlyList<string>[] groups) => [.. groups.SelectMany(group => group)];

    /// <summary>이름이 접두사와 같거나 <c>접두사.</c>로 시작하면 <see langword="true"/>.</summary>
    /// <param name="name">어셈블리 · 패키지 · 네임스페이스 이름.</param>
    /// <param name="prefix">금지 접두사.</param>
    /// <returns>판별 결과.</returns>
    public static bool Matches(string name, string prefix) =>
        string.Equals(name, prefix, StringComparison.Ordinal) || name.StartsWith(prefix + ".", StringComparison.Ordinal);
}
