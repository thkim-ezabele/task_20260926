using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmergencyHub.Employee.Infrastructure.Persistence;

/// <summary>
/// <c>dotnet ef</c> 설계 시점 팩터리입니다(database.md "설계 시점 팩터리"). 쓰기 DbContext만 만듭니다.
/// </summary>
/// <remarks>
/// 연결 문자열은 환경 변수 <c>ConnectionStrings__Write</c>, 없으면 비밀 없는 더미 값입니다. 마이그레이션 생성 · 스크립트는 연결을 열지 않습니다.
/// 공통 옵션 구성을 써서 모델이 등록 경로와 같고, 감사 인터셉터는 붙이지 않습니다. 한 어셈블리에 DbContext가 2개라 <c>--context EmployeeDbContext</c>가 필요합니다.
/// </remarks>
internal sealed class EmployeeDbContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
{
    internal const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Write";

    internal const string DummyConnectionString = "Host=localhost;Database=emergency_hub_employee;Username=employee_app";

    public EmployeeDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseBuildingBlocksNpgsql(ResolveConnectionString(Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)))
            .Options);

    internal static string ResolveConnectionString(string? environmentValue) =>
        string.IsNullOrWhiteSpace(environmentValue) ? DummyConnectionString : environmentValue;
}
