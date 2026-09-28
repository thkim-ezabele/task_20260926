using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: 초기 마이그레이션은 InitialCreate 1건이고, 스냅샷이 현재 모델과 같아야 한다(매핑을 바꾸고 마이그레이션을 안 만들면 실패).
// idempotent 스크립트는 연결 없이 만든다. 실제 적용(MigrateAsync 2회, psql 2회)은 S03-T05 · T06.
// S05-T04: 기대값은 새 스키마 명세(database.md)로 먼저 바꿨다. InitialCreate 재생성(S05-T05) 전까지 ModelSnapshot · IdempotentScript 2건은
// 옛 마이그레이션과 달라 실패한다(S05-T04 실패 허용 목록). 문자열 형식이 T05 생성 SQL과 다르면 기대 문자열을 실측값에 맞춘다.
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

    // coding-conventions "클래스는 기본 sealed"(ConventionRules.ClassesAreSealed): 생성 파일은 고치지 않고 *.Sealed.cs partial 선언으로 봉인한다.
    // 대상이 0개면 검사가 무의미하므로 개수(마이그레이션 1 + 스냅샷 1)를 함께 단언한다.
    [Fact]
    public void MigrationAndSnapshotTypes_InInfrastructureAssembly_AreAllSealed()
    {
        var types = EmployeeInfrastructureAssembly.Assembly.GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) || typeof(ModelSnapshot).IsAssignableFrom(type))
            .ToList();

        types.Should().HaveCount(2);
        types.Should().OnlyContain(type => type.IsSealed);
    }

    // 엣지: sealed여도 EF가 마이그레이션 형식을 찾아 인스턴스를 만든다(MigrateAsync · script 경로와 같은 IMigrationsAssembly).
    [Fact]
    public void SealedInitialCreate_IsDiscoveredAndCreatedByMigrationsAssembly()
    {
        var migrationsAssembly = _context.GetService<IMigrationsAssembly>();
        var migrationType = migrationsAssembly.Migrations.Should().ContainSingle().Which.Value;

        var migration = migrationsAssembly.CreateMigration(migrationType, _context.Database.ProviderName!);

        migrationType.IsSealed.Should().BeTrue();
        migration.TargetModel.Should().NotBeNull();
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

        script.Should().Contain(EmployeeCreateScript.CreateTableHeader);
        script.Should().Contain("CONSTRAINT pk_employees PRIMARY KEY (id)");
        script.Should().Contain("CONSTRAINT ck_employees_employee_status CHECK (employee_status IN (1, 2))");
        script.Should().Contain(EmployeeCreateScript.UniqueIndexSql);
        script.Should().Contain(EmployeeCreateScript.JoinedOnIdIndexSql);
        script.Should().Contain(EmployeeCreateScript.NameJoinedOnIdIndexSql);
        script.Should().Contain("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\"");
        EmployeeCreateScript.TableLines(script).Should().Equal(EmployeeCreateScript.ExpectedTableLines);
        EmployeeCreateScript.Count(script, "CREATE INDEX ").Should().Be(2, "ix_ 인덱스는 2개뿐이다(email 인덱스 · 옛 유니크 인덱스 없음)");
        EmployeeCreateScript.Count(script, "CREATE UNIQUE INDEX ").Should().Be(1);
        script.Should().NotContain("public.");
        script.Should().NotContain("xmin");
        script.Should().NotContain("DEFAULT");
        script.Should().NotContain("CONCURRENTLY");
    }
}
