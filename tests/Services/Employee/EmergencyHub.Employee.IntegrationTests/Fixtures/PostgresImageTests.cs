using System.Reflection;

namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

// S03-T06 · TD-004: fixture 이미지 태그는 Directory.Build.props 한 곳이 원본이고 어셈블리 메타데이터로만 읽는다(AppHost와 같은 태그).
[Trait("FR", "PRD-001/FR-09")]
public sealed class PostgresImageTests
{
    // ---- 성공 ----

    [Fact]
    public void Name_TestAssembly_UsesTagFromDirectoryBuildProps()
    {
        var expected = RepositoryFiles.BuildProperty("EmergencyHubPostgresImageTag");

        var name = PostgresImage.Name;

        expected.Should().NotBeNullOrWhiteSpace();
        name.Should().Be($"postgres:{expected}");
    }

    // ---- 실패 ----

    [Fact]
    public void Read_AssemblyWithoutMetadata_Throws()
    {
        var act = () => PostgresImage.Read(typeof(FactAttribute).Assembly);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{PostgresImage.MetadataKey}*");
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BlankValue_Throws(string? value)
    {
        var act = () => PostgresImage.Parse([new AssemblyMetadataAttribute(PostgresImage.MetadataKey, value)]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_TwoEntries_Throws()
    {
        var act = () => PostgresImage.Parse([new(PostgresImage.MetadataKey, "17"), new(PostgresImage.MetadataKey, "16")]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*2*");
    }
}
