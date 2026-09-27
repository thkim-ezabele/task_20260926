using EmergencyHub.Employee.Infrastructure.Persistence;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02 · database.md "설계 시점 팩터리": 쓰기 DbContext만 만들고, 연결 문자열은 환경 변수(ConnectionStrings__Write) 또는 더미 값(비밀 없음).
// 공통 옵션 구성을 쓰므로 모델이 등록 경로와 같고, 감사 인터셉터는 붙이지 않는다(마이그레이션 생성에 필요 없음).
// 환경 변수는 프로세스 전역이라 테스트가 바꾸지 않고, 해석 규칙은 순수 함수로 확인한다.
[Trait("FR", "PRD-001/FR-09")]
public sealed class EmployeeDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_Called_ReturnsNpgsqlWriteContextWithSameModelAsRegisteredPath()
    {
        using var created = new EmployeeDbContextFactory().CreateDbContext([]);
        using var reference = EmployeeDbContexts.CreateWrite();

        created.Database.IsNpgsql().Should().BeTrue();
        created.Database.CreateExecutionStrategy().RetriesOnFailure.Should().BeTrue();
        created.Database.GenerateCreateScript().Should().Be(reference.Database.GenerateCreateScript());
    }

    [Fact]
    public void CreateDbContext_Called_AddsNoInterceptors()
    {
        using var created = new EmployeeDbContextFactory().CreateDbContext([]);

        created.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!.Interceptors.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ResolveConnectionString_EnvironmentValueSet_UsesIt()
    {
        const string FromEnvironment = "Host=db.local;Database=emergency_hub_employee;Username=employee_app";

        EmployeeDbContextFactory.ResolveConnectionString(FromEnvironment).Should().Be(FromEnvironment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveConnectionString_EnvironmentValueMissingOrBlank_FallsBackToDummyWithoutPassword(string? environmentValue)
    {
        var resolved = EmployeeDbContextFactory.ResolveConnectionString(environmentValue);

        resolved.Should().Be(EmployeeDbContextFactory.DummyConnectionString);
        resolved.Should().NotContainEquivalentOf("password");
        resolved.Should().Contain("Database=emergency_hub_employee").And.Contain("Username=employee_app");
    }

    [Fact]
    public void ConnectionStringEnvironmentVariable_IsWriteKeyInEnvironmentForm()
    {
        EmployeeDbContextFactory.ConnectionStringEnvironmentVariable.Should().Be("ConnectionStrings__Write");
    }
}
