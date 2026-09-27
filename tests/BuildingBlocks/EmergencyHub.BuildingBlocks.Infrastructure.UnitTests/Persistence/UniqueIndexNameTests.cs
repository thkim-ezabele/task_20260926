using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// S02-T04: ux_ 이름 형식. 서비스가 이름 상수를 소유하고, 매핑(HasUniqueIndex)과 23505 매핑 레지스트리(S02-T07)가 같은 값을 쓴다.
// 잘린 이름(63바이트 초과)은 23505 ConstraintName과 일치하지 않으므로 만들 때 거부한다(database.md).
[Trait("FR", "PRD-001/FR-06")]
public sealed class UniqueIndexNameTests
{
    // ---- 성공 ----

    [Fact]
    public void Constructor_ValidName_KeepsValueAndToStringReturnsIt()
    {
        var name = new UniqueIndexName("ux_employees_email");

        name.Value.Should().Be("ux_employees_email");
        name.ToString().Should().Be("ux_employees_email");
    }

    [Fact]
    public void Equality_SameValue_IsEqualSoRegistryLookupWorks()
    {
        new UniqueIndexName("ux_employees_email").Should().Be(new UniqueIndexName("ux_employees_email"));
        new UniqueIndexName("ux_employees_email").GetHashCode().Should().Be(new UniqueIndexName("ux_employees_email").GetHashCode());
    }

    // ---- 엣지 ----

    [Fact]
    public void Constructor_Exactly63Bytes_IsAccepted()
    {
        var value = "ux_" + new string('a', 60);

        new UniqueIndexName(value).Value.Should().HaveLength(63);
    }

    [Fact]
    public void Constructor_DigitsAndUnderscores_AreAccepted()
    {
        new UniqueIndexName("ux_contacts_employee_id_2").Value.Should().Be("ux_contacts_employee_id_2");
    }

    // ---- 실패 ----

    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        var act = () => new UniqueIndexName(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Over63Bytes_ThrowsBecausePostgreSqlWouldTruncate()
    {
        var act = () => new UniqueIndexName("ux_" + new string('a', 61));

        act.Should().Throw<ArgumentException>().WithMessage("*63*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ux_")]
    [InlineData("ix_employees_email")]
    [InlineData("employees_email")]
    [InlineData("UX_employees_email")]
    [InlineData("ux_Employees_Email")]
    [InlineData("ux_employees-email")]
    [InlineData("ux_employees email")]
    [InlineData(" ux_employees_email")]
    [InlineData("ux_직원_이메일")]
    public void Constructor_NotLowerSnakeCaseWithUxPrefix_ThrowsArgumentException(string value)
    {
        var act = () => new UniqueIndexName(value);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("value");
    }
}
