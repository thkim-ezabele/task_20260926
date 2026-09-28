using EmergencyHub.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.ArchitectureTests.Samples.Fixtures;

/// <summary>표본용 읽기 DbContext. 형식 메타데이터만 쓰고 만들지 않는다.</summary>
/// <param name="options">옵션.</param>
public sealed class SampleReadDbContext(DbContextOptions<SampleReadDbContext> options) : ReadDbContextBase(options)
{
    /// <inheritdoc />
    protected override IDbModelDefinition ModelDefinition => throw new NotSupportedException();
}
