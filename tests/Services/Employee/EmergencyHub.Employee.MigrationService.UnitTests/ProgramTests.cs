using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmergencyHub.Employee.MigrationService.UnitTests;

// S03-T03 · database.md "MigrationService 동작 사양" 1 · 6 · 8: ServiceDefaults + 쓰기 DbContext만 등록(읽기 · UnitOfWork 없음),
// ConnectionStrings:Read 없이 시작, Write가 없으면 호스트 생성 전 예외(값 미노출). 연결은 열지 않는다.
[Trait("FR", "PRD-001/FR-09")]
public sealed class ProgramTests
{
    private const string DummyWrite = "Host=localhost;Database=emergency_hub_employee;Username=employee_app";

    [Fact]
    public void Configure_WriteConnectionOnly_BuildsHostWithWriteContextAndWorker()
    {
        var builder = CreateBuilder(new Dictionary<string, string?> { ["ConnectionStrings:Write"] = DummyWrite });

        Program.Configure(builder);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<EmployeeDbContext>().Should().NotBeNull();
        host.Services.GetServices<IHostedService>().Should().ContainSingle(service => service is MigrationWorker);
    }

    [Fact]
    public void Configure_Called_RegistersNoReadContextOrUnitOfWork()
    {
        var builder = CreateBuilder(new Dictionary<string, string?> { ["ConnectionStrings:Write"] = DummyWrite });

        Program.Configure(builder);

        builder.Services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(EmployeeReadDbContext));
        builder.Services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IUnitOfWork));
    }

    [Fact]
    public void Configure_WriteConnectionMissing_ThrowsWithoutConnectionValues()
    {
        // Read만 있어도 Write가 없으면 시작하지 않는다(메시지에 다른 연결 값 없음).
        var builder = CreateBuilder(new Dictionary<string, string?> { ["ConnectionStrings:Read"] = "Host=db;Password=do-not-leak" });

        var act = () => Program.Configure(builder);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:Write*")
            .Which.Message.Should().NotContain("do-not-leak");
    }

    [Fact]
    public void Configure_WriteConnectionWhitespace_Throws()
    {
        var builder = CreateBuilder(new Dictionary<string, string?> { ["ConnectionStrings:Write"] = "   " });

        var act = () => Program.Configure(builder);

        act.Should().Throw<InvalidOperationException>();
    }

    private static HostApplicationBuilder CreateBuilder(IDictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = "Testing",
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Configuration.AddInMemoryCollection(settings);
        return builder;
    }
}
