using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04 A13: 쓰기 · 읽기 DbContext는 같은 공통 규칙 + 같은 모델 정의를 적용하므로 관계형 모델(테이블 · 컬럼 · 제약)이 같아야 한다.
// 읽기 모델이 다르면 Read Repository 프로젝션이 실제 스키마와 어긋난다(database.md "EF Core 공통 모델 규칙").
[Trait("FR", "PRD-001/FR-06")]
public sealed class ReadWriteModelParityTests
{
    [Fact]
    public void RelationalModels_OfWriteAndReadContexts_AreIdentical()
    {
        using var write = SampleDbContexts.CreateWrite();
        using var read = SampleDbContexts.CreateRead();

        var writeSnapshot = Describe(write);
        var readSnapshot = Describe(read);

        writeSnapshot.Should().NotBeEmpty();
        readSnapshot.Should().Equal(writeSnapshot);
    }

    [Fact]
    public void CreateScripts_OfWriteAndReadContexts_AreIdentical()
    {
        using var write = SampleDbContexts.CreateWrite();
        using var read = SampleDbContexts.CreateRead();

        read.Database.GenerateCreateScript().Should().Be(write.Database.GenerateCreateScript());
    }

    private static List<string> Describe(DbContext context)
    {
        var model = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();

        return model.Tables
            .SelectMany(table =>
                table.Columns.Select(column => $"{table.Name}.column {column.Name} {column.StoreType} null={column.IsNullable}")
                    .Concat(table.PrimaryKey is null ? [] : [$"{table.Name}.pk {table.PrimaryKey.Name}"])
                    .Concat(table.UniqueConstraints.Select(constraint => $"{table.Name}.unique {constraint.Name}"))
                    .Concat(table.ForeignKeyConstraints.Select(constraint => $"{table.Name}.fk {constraint.Name}"))
                    .Concat(table.Indexes.Select(index => $"{table.Name}.index {index.Name} unique={index.IsUnique}"))
                    .Concat(table.CheckConstraints.Select(check => $"{table.Name}.check {check.Name} {check.Sql}"))
                    .Concat(table.EntityTypeMappings.SelectMany(mapping => mapping.TypeBase.GetProperties()
                        .Where(property => property.IsConcurrencyToken)
                        .Select(property => $"{table.Name}.token {mapping.TypeBase.DisplayName()}.{property.Name}"))))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
