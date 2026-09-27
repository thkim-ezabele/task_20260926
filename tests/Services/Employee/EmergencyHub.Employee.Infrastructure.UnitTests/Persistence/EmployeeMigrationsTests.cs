using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: 초기 마이그레이션은 InitialCreate 1건이고, 스냅샷이 현재 모델과 같아야 한다(매핑을 바꾸고 마이그레이션을 안 만들면 실패).
// idempotent 스크립트는 연결 없이 만든다. 실제 적용(MigrateAsync 2회, psql 2회)은 S03-T05 · T06.
[Trait("FR", "PRD-001/FR-09")]
public sealed class EmployeeMigrationsTests : IDisposable
{
    private readonly DbContext _context = EmployeeDbContexts.CreateWrite();

    public void Dispose() => _context.Dispose();

    [Fact]
    public void Migrations_OfWriteContext_AreOnlyInitialCreate()
    {
        _context.Database.GetMigrations().Should().ContainSingle().Which.Should().EndWith("_InitialCreate");
    }

    [Fact]
    public void ModelSnapshot_HasNoDifferencesFromCurrentModel()
    {
        var snapshot = _context.GetService<IMigrationsAssembly>().ModelSnapshot;
        snapshot.Should().NotBeNull();
        var snapshotModel = snapshot!.Model is IMutableModel mutable ? mutable.FinalizeModel() : snapshot.Model;
        snapshotModel = _context.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);

        var differences = _context.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            _context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        differences.Should().BeEmpty("매핑이 바뀌었으면 새 마이그레이션이 필요하다");
    }

    [Fact]
    public void IdempotentScript_CreatesEmployeesAndHistoryWithoutSchemaXminOrDefault()
    {
        var script = _context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        script.Should().Contain("CREATE TABLE employees (");
        script.Should().Contain("CREATE UNIQUE INDEX ux_employees_email ON employees (email);");
        script.Should().Contain("CONSTRAINT ck_employees_employee_status CHECK (employee_status IN (1, 2))");
        script.Should().Contain("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\"");
        script.Should().NotContain("public.");
        script.Should().NotContain("xmin");
        script.Should().NotContain("DEFAULT");
        script.Should().NotContain("ix_");
    }
}
