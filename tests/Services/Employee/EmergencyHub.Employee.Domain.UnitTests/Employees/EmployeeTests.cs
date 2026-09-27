using System.Globalization;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Domain.Employees.Events;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S03-T01 Employee Aggregate: 팩토리 Register(id, displayName, email, status)에서 이름 Trim, 이메일 Trim + ToLowerInvariant 정규화.
// 불변식 위반은 예외(계획 리뷰 결정 "Aggregate 불변식 위반은 예외"). 입력 오류의 Result(21001 ~ 21006)는 Validator 몫이다.
// 길이는 string.Length(UTF-16 코드 단위) 기준이다. 이모지는 한 글자가 2로 세어져 varchar(n) 문자 수보다 엄격하다(인계 메모).
public sealed class EmployeeTests
{
    private const string EmailDomain = "@example.com";

    public static TheoryData<string> BlankValues() => new(string.Empty, " ", "   ", "\t", "\r\n", "　");

    public static TheoryData<string> DisplayNamesAtMaxLength() => new(
        new string('a', 100),
        new string('가', 100),
        string.Concat(Enumerable.Repeat("😀", 50)),
        "  " + new string('가', 100) + "  ");

    public static TheoryData<string> DisplayNamesOverMaxLength() => new(
        new string('a', 101),
        new string('가', 101),
        "a" + string.Concat(Enumerable.Repeat("😀", 50)));

    public static TheoryData<EmployeeId> EmptyIds() => new(default(EmployeeId), new EmployeeId(Guid.Empty));

    // ---- 성공 ----

    [Fact]
    public void Register_WithValidInput_SetsAllProperties()
    {
        var employee = new EmployeeBuilder().Build();

        employee.Id.Should().Be(EmployeeBuilder.DefaultId);
        employee.DisplayName.Should().Be(EmployeeBuilder.DefaultDisplayName);
        employee.Email.Should().Be(EmployeeBuilder.DefaultEmail);
        employee.EmployeeStatus.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public void Register_WithValidInput_RaisesSingleRegisteredDomainEventWithId()
    {
        var employee = new EmployeeBuilder().Build();

        employee.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new EmployeeRegisteredDomainEvent(EmployeeBuilder.DefaultId));
    }

    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.Inactive)]
    public void Register_WithEachDefinedStatus_StoresStatus(EmployeeStatus status)
    {
        var employee = new EmployeeBuilder().WithStatus(status).Build();

        employee.EmployeeStatus.Should().Be(status);
    }

    // ---- 엣지: 이름 ----

    [Theory]
    [InlineData("  홍길동  ", "홍길동")]
    [InlineData("\t홍 길동\n", "홍 길동")]
    [InlineData("Hong Gil-dong", "Hong Gil-dong")]
    public void Register_DisplayNameWithSurroundingWhitespace_StoresTrimmedValueKeepingInnerSpaces(string input, string expected)
    {
        var employee = new EmployeeBuilder().WithDisplayName(input).Build();

        employee.DisplayName.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(DisplayNamesAtMaxLength))]
    public void Register_DisplayNameAtMaxLength_Succeeds(string displayName)
    {
        var employee = new EmployeeBuilder().WithDisplayName(displayName).Build();

        employee.DisplayName.Should().Be(displayName.Trim());
        employee.DisplayName.Length.Should().Be(Domain.Employees.Employee.DisplayNameMaxLength);
    }

    [Theory]
    [InlineData("가")]
    [InlineData("😀")]
    public void Register_DisplayNameAtMinimumLength_Succeeds(string displayName)
    {
        var employee = new EmployeeBuilder().WithDisplayName(displayName).Build();

        employee.DisplayName.Should().Be(displayName);
    }

    // ---- 엣지: 이메일 ----

    [Theory]
    [InlineData("Hong@Example.COM", "hong@example.com")]
    [InlineData("  hong@example.com  ", "hong@example.com")]
    [InlineData(" A@X.example.com ", "a@x.example.com")]
    [InlineData("hong@example.com", "hong@example.com")]
    public void Register_Email_StoresTrimmedLowerInvariantValue(string input, string expected)
    {
        var employee = new EmployeeBuilder().WithEmail(input).Build();

        employee.Email.Should().Be(expected);
    }

    [Fact]
    public void Register_EmailWithTurkishDottedCapitalI_KeepsItOneToOne()
    {
        // 'İ'(U+0130)는 문화권마다 소문자 규칙이 다르다(tr: 'i', 전체 매핑: 'i' + U+0307).
        // .NET ToLowerInvariant는 U+0130을 그대로 두는 1:1 변환이라 길이가 바뀌지 않는다(실측, 운영체제 무관).
        const string TurkishEmail = "İstanbul@example.com";

        var employee = new EmployeeBuilder().WithEmail(TurkishEmail).Build();

        employee.Email.Should().Be("İstanbul@example.com");
        employee.Email.Length.Should().Be(TurkishEmail.Length);
    }

    [Fact]
    public void Register_EmailUnderTurkishCulture_LowercasesAsciiIWithoutDotlessI()
    {
        // 현재 문화권이 tr-TR이어도 'I'는 'ı'(U+0131)가 아니라 'i'가 된다. 정규화 결과가 서버 문화권에 따라 달라지면 중복 검사가 깨진다.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        try
        {
            var employee = new EmployeeBuilder().WithEmail("KIM@EXAMPLE.COM").Build();

            employee.Email.Should().Be("kim@example.com");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Register_EmailAtMaxLength_Succeeds()
    {
        var email = new string('a', 254 - EmailDomain.Length) + EmailDomain;

        var employee = new EmployeeBuilder().WithEmail(email).Build();

        employee.Email.Length.Should().Be(Domain.Employees.Employee.EmailMaxLength);
    }

    [Fact]
    public void Register_EmailAtMaxLengthWithSurroundingWhitespace_SucceedsBecauseLengthIsMeasuredAfterTrim()
    {
        var email = "  " + new string('a', 254 - EmailDomain.Length) + EmailDomain + "  ";

        var employee = new EmployeeBuilder().WithEmail(email).Build();

        employee.Email.Length.Should().Be(254);
    }

    // ---- 실패: ID ----

    [Theory]
    [MemberData(nameof(EmptyIds))]
    public void Register_EmptyId_ThrowsArgumentException(EmployeeId emptyId)
    {
        var act = () => new EmployeeBuilder().WithId(emptyId).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    // ---- 실패: 이름 ----

    [Fact]
    public void Register_NullDisplayName_ThrowsArgumentNullException()
    {
        var act = () => new EmployeeBuilder().WithDisplayName(null!).Build();

        act.Should().Throw<ArgumentNullException>().WithParameterName("displayName");
    }

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Register_BlankDisplayName_ThrowsArgumentException(string blankDisplayName)
    {
        var act = () => new EmployeeBuilder().WithDisplayName(blankDisplayName).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Theory]
    [MemberData(nameof(DisplayNamesOverMaxLength))]
    public void Register_DisplayNameOverMaxLength_ThrowsArgumentException(string tooLongDisplayName)
    {
        var act = () => new EmployeeBuilder().WithDisplayName(tooLongDisplayName).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    // ---- 실패: 이메일 ----

    [Fact]
    public void Register_NullEmail_ThrowsArgumentNullException()
    {
        var act = () => new EmployeeBuilder().WithEmail(null!).Build();

        act.Should().Throw<ArgumentNullException>().WithParameterName("email");
    }

    [Theory]
    [MemberData(nameof(BlankValues))]
    public void Register_BlankEmail_ThrowsArgumentException(string blankEmail)
    {
        var act = () => new EmployeeBuilder().WithEmail(blankEmail).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Fact]
    public void Register_EmailOverMaxLength_ThrowsArgumentException()
    {
        var email = new string('a', 255 - EmailDomain.Length) + EmailDomain;

        var act = () => new EmployeeBuilder().WithEmail(email).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Theory]
    [InlineData("hong")]
    [InlineData("@example.com")]
    [InlineData("hong@")]
    [InlineData("hong@@example.com")]
    [InlineData("a@b@example.com")]
    public void Register_MalformedEmail_ThrowsArgumentException(string malformedEmail)
    {
        var act = () => new EmployeeBuilder().WithEmail(malformedEmail).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    // ---- 실패: 상태 ----

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)3)]
    [InlineData((short)99)]
    [InlineData((short)-1)]
    public void Register_ReservedOrUndefinedStatus_ThrowsArgumentOutOfRangeException(short rawStatus)
    {
        var act = () => new EmployeeBuilder().WithStatus((EmployeeStatus)rawStatus).Build();

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("status");
    }

    // ---- Deactivate ----

    [Fact]
    public void Deactivate_ActiveEmployee_BecomesInactive()
    {
        var employee = new EmployeeBuilder().WithStatus(EmployeeStatus.Active).Build();

        employee.Deactivate();

        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
    }

    [Fact]
    public void Deactivate_ActiveEmployee_DoesNotChangeOtherPropertiesOrRaiseEvents()
    {
        var employee = new EmployeeBuilder().Build();
        employee.ClearDomainEvents();

        employee.Deactivate();

        employee.Id.Should().Be(EmployeeBuilder.DefaultId);
        employee.DisplayName.Should().Be(EmployeeBuilder.DefaultDisplayName);
        employee.Email.Should().Be(EmployeeBuilder.DefaultEmail);
        employee.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_IsNoOp()
    {
        // 같은 상태로의 전이는 멱등(변경 없음)으로 정했다. Command · API가 없어 오류 코드를 두지 않는다(S03-T01 developer 결정).
        var employee = new EmployeeBuilder().WithStatus(EmployeeStatus.Inactive).Build();
        employee.ClearDomainEvents();

        var act = employee.Deactivate;

        act.Should().NotThrow();
        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
        employee.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_CalledTwice_StaysInactive()
    {
        var employee = new EmployeeBuilder().Build();
        employee.Deactivate();

        employee.Deactivate();

        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
    }
}
