using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

/// <remarks>
/// 공통 옵션 구성(UseBuildingBlocksNpgsql)과 더미 연결 문자열로 Employee DbContext를 만든다. 등록 확장 · 설계 시점 팩터리와 같은 옵션 경로다.
/// 모델 생성 · 스크립트 생성은 연결을 열지 않는다. 쿼리는 FakeQueryDatabase를 인터셉터로 넘겨 DB 없이 실행한다.
/// </remarks>
internal static class EmployeeDbContexts
{
    public const string DummyConnectionString = "Host=localhost;Database=emergency_hub_employee;Username=employee_app";

    public static EmployeeDbContext CreateWrite(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseBuildingBlocksNpgsql(DummyConnectionString)
            .AddInterceptors(interceptors)
            .Options);

    public static EmployeeReadDbContext CreateRead(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<EmployeeReadDbContext>()
            .UseBuildingBlocksNpgsql(DummyConnectionString)
            .AddInterceptors(interceptors)
            .Options);

    public static EmployeeAggregate NewEmployee(string email = "hong@example.com") =>
        EmployeeAggregate.Register(
            new EmployeeId(Guid.NewGuid()),
            Name.Create("홍길동").Value,
            Email.Create(email).Value,
            PhoneNumber.Create("010-1234-5678").Value,
            JoinedOn.Create("2020-03-02").Value);
}
