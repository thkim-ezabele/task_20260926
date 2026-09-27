using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.Persistence;

// S03-T02: 쓰기 · 읽기 DbContext는 같은 EmployeeModelDefinition 인스턴스와 공통 규칙을 쓰므로 관계형 모델이 같다.
// 읽기 모델이 다르면 Read Repository 프로젝션이 마이그레이션으로 만든 스키마와 어긋난다.
[Trait("FR", "PRD-001/FR-06")]
public sealed class EmployeeReadWriteModelParityTests
{
    [Fact]
    public void RelationalModels_OfWriteAndReadContexts_AreIdentical()
    {
        using var write = EmployeeDbContexts.CreateWrite();
        using var read = EmployeeDbContexts.CreateRead();

        var writeSnapshot = Describe(write);

        writeSnapshot.Should().NotBeEmpty();
        Describe(read).Should().Equal(writeSnapshot);
    }

    [Fact]
    public void CreateScripts_OfWriteAndReadContexts_AreIdentical()
    {
        using var write = EmployeeDbContexts.CreateWrite();
        using var read = EmployeeDbContexts.CreateRead();

        read.Database.GenerateCreateScript().Should().Be(write.Database.GenerateCreateScript());
    }

    [Fact]
    public void ReadContext_DefaultsToNoTrackingAndRejectsSaving()
    {
        using var read = EmployeeDbContexts.CreateRead();

        read.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        read.Invoking(context => context.SaveChanges()).Should().Throw<InvalidOperationException>();
    }

    private static List<string> Describe(DbContext context) =>
        context.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables
            .SelectMany(table =>
                table.Columns.Select(column => $"{table.Name}.column {column.Name} {column.StoreType} null={column.IsNullable}")
                    .Concat([$"{table.Name}.pk {table.PrimaryKey?.Name}"])
                    .Concat(table.Indexes.Select(index => $"{table.Name}.index {index.Name} unique={index.IsUnique}"))
                    .Concat(table.CheckConstraints.Select(check => $"{table.Name}.check {check.Name} {check.Sql}"))
                    .Concat(table.EntityTypeMappings.SelectMany(mapping => mapping.TypeBase.GetProperties()
                        .Where(property => property.IsConcurrencyToken)
                        .Select(property => $"{table.Name}.token {property.Name}"))))
            .Order(StringComparer.Ordinal)
            .ToList();
}
