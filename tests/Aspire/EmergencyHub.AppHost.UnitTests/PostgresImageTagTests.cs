using System.Reflection;

namespace EmergencyHub.AppHost.UnitTests;

// S03-T05: PostgreSQL 이미지 태그는 Directory.Build.props 한 곳이 원본이고, AppHost는 어셈블리 메타데이터로만 읽는다(코드에 태그 리터럴 없음).
[Trait("FR", "PRD-001/FR-03")]
public sealed class PostgresImageTagTests
{
    private const string OtherKey = "SomethingElse";

    // ---- 성공 ----

    [Fact]
    public void Read_AppHostAssembly_ReturnsValueFromDirectoryBuildProps()
    {
        var expected = RepositoryFiles.BuildProperty("EmergencyHubPostgresImageTag");

        var tag = PostgresImageTag.Read(typeof(EmergencyHubApplication).Assembly);

        expected.Should().NotBeNullOrWhiteSpace();
        tag.Should().Be(expected);
    }

    [Fact]
    public void Parse_SingleEntryAmongOtherMetadata_ReturnsItsValue()
    {
        var tag = PostgresImageTag.Parse([new(OtherKey, "x"), new(PostgresImageTag.MetadataKey, "test-tag")]);

        tag.Should().Be("test-tag");
    }

    // ---- 실패 ----

    [Fact]
    public void Read_AssemblyWithoutMetadata_Throws()
    {
        var act = () => PostgresImageTag.Read(typeof(PostgresImageTagTests).Assembly);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{PostgresImageTag.MetadataKey}*");
    }

    [Fact]
    public void Parse_OnlyOtherKeys_Throws()
    {
        var act = () => PostgresImageTag.Parse([new AssemblyMetadataAttribute(OtherKey, "other-tag")]);

        act.Should().Throw<InvalidOperationException>();
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BlankValue_Throws(string? value)
    {
        var act = () => PostgresImageTag.Parse([new AssemblyMetadataAttribute(PostgresImageTag.MetadataKey, value)]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_DuplicateKey_Throws()
    {
        var act = () => PostgresImageTag.Parse([new(PostgresImageTag.MetadataKey, "test-tag"), new(PostgresImageTag.MetadataKey, "other-tag")]);

        act.Should().Throw<InvalidOperationException>("태그 원본은 한 곳이어야 한다");
    }
}
