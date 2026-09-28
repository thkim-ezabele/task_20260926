using System.Reflection;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Domain.Employees.Events;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Domain.UnitTests.Employees;

// S05-T04 Employee Aggregate: Register(id, Name, Email, PhoneNumber, JoinedOn)는 검증된 Value Object만 받는다(PRD-002 FR-01, ADR-0026 8절).
// 필드 규칙의 Result(21003 ~ 21005, 21007 ~ 21017)는 VO Create 몫이고(VO 테스트), Aggregate 불변식 위반(빈 ID · null VO)은 예외다
// (coding-conventions "실패 처리 경계"). employee_status는 등록 시 Active=1 고정이고 입력으로 받지 않는다(PRD-002 Q13).
[Trait("FR", "PRD-002/FR-01")]
public sealed class EmployeeTests
{
    public static TheoryData<EmployeeId> EmptyIds() => new(default(EmployeeId), new EmployeeId(Guid.Empty));

    // ---- 성공 ----

    [Fact]
    public void Register_WithValidValueObjects_SetsIdAndEachValueObject()
    {
        var name = Name.Create(EmployeeBuilder.DefaultName).Value;
        var email = Email.Create("hong@example.com").Value;
        var phoneNumber = PhoneNumber.Create(EmployeeBuilder.DefaultPhoneNumber).Value;
        var joinedOn = JoinedOn.Create(EmployeeBuilder.DefaultJoinedOn).Value;

        var employee = EmployeeAggregate.Register(EmployeeBuilder.DefaultId, name, email, phoneNumber, joinedOn);

        employee.Id.Should().Be(EmployeeBuilder.DefaultId);
        employee.Name.Should().BeSameAs(name);
        employee.Email.Should().BeSameAs(email);
        employee.PhoneNumber.Should().BeSameAs(phoneNumber);
        employee.JoinedOn.Should().BeSameAs(joinedOn);
    }

    [Fact]
    public void Register_WithValidValueObjects_FixesStatusToActive()
    {
        var employee = new EmployeeBuilder().Build();

        employee.EmployeeStatus.Should().Be(EmployeeStatus.Active);
        ((short)employee.EmployeeStatus).Should().Be(1);
    }

    [Fact]
    public void Register_WithValidValueObjects_RaisesSingleRegisteredDomainEventWithIdOnly()
    {
        var employee = new EmployeeBuilder().Build();

        employee.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new EmployeeRegisteredDomainEvent(EmployeeBuilder.DefaultId));
    }

    [Fact]
    public void Register_Email_StoresNormalizedEmailFromEmailValueObject()
    {
        var email = Email.Create("  Hong@Example.COM  ").Value;

        var employee = new EmployeeBuilder().WithEmail("  Hong@Example.COM  ").Build();

        employee.Email.Value.Should().Be("Hong@Example.COM", "입력 표기(Trim만)를 보존한다");
        employee.NormalizedEmail.Should().Be("hong@example.com");
        employee.NormalizedEmail.Should().Be(email.NormalizedEmail);
    }

    // ---- 엣지 ----

    [Fact]
    public void Register_EmailsDifferingOnlyInCase_HaveSameNormalizedEmailButKeepEachNotation()
    {
        var lower = new EmployeeBuilder().WithEmail("hong@example.com").Build();
        var upper = new EmployeeBuilder().WithEmail("HONG@EXAMPLE.COM").Build();

        upper.NormalizedEmail.Should().Be(lower.NormalizedEmail);
        upper.Email.Should().NotBe(lower.Email);
    }

    [Fact]
    public void Register_EmailWithTurkishDottedCapitalI_KeepsItInNormalizedEmail()
    {
        // .NET ToLowerInvariant는 U+0130을 바꾸지 않는다(ADR-0027, S05-T02 실측). NormalizedEmail은 Email VO 값을 그대로 쓴다.
        var employee = new EmployeeBuilder().WithEmail("İstanbul@Example.com").Build();

        employee.NormalizedEmail.Should().Be("İstanbul@example.com");
    }

    [Fact]
    public void Register_NfdName_StoresNfcNameFromValueObject()
    {
        const string Nfd = "홍길동";

        var employee = new EmployeeBuilder().WithName(Nfd).Build();

        employee.Name.Value.Should().Be("홍길동");
    }

    [Fact]
    public void Register_Signature_TakesOnlyIdAndValueObjectsWithoutStatus()
    {
        // 상태는 입력으로 받지 않는다(Active 고정 · API 비노출, PRD-002 Q13). 옛 샘플의 (string, string, EmployeeStatus) 오버로드도 없다.
        var overloads = typeof(EmployeeAggregate).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(EmployeeAggregate.Register))
            .ToList();

        overloads.Should().ContainSingle()
            .Which.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(EmployeeId), typeof(Name), typeof(Email), typeof(PhoneNumber), typeof(JoinedOn));
    }

    [Fact]
    public void PublicProperties_AreDeclaredInColumnOrder()
    {
        // 열 순서는 속성 선언 순서를 따른다(database.md 새 스키마 명세). 메타데이터 테스트가 생성 SQL로 한 번 더 확인한다.
        typeof(EmployeeAggregate).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Should().Equal(
                nameof(EmployeeAggregate.Name),
                nameof(EmployeeAggregate.Email),
                nameof(EmployeeAggregate.NormalizedEmail),
                nameof(EmployeeAggregate.PhoneNumber),
                nameof(EmployeeAggregate.JoinedOn),
                nameof(EmployeeAggregate.EmployeeStatus));
    }

    // ---- 실패: 불변식(예외) ----

    [Theory]
    [MemberData(nameof(EmptyIds))]
    public void Register_EmptyId_ThrowsArgumentException(EmployeeId emptyId)
    {
        var act = () => new EmployeeBuilder().WithId(emptyId).Build();

        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    [InlineData("joinedOn")]
    public void Register_NullValueObject_ThrowsArgumentNullException(string parameterName)
    {
        var name = parameterName == "name" ? null : Name.Create(EmployeeBuilder.DefaultName).Value;
        var email = parameterName == "email" ? null : Email.Create("hong@example.com").Value;
        var phoneNumber = parameterName == "phoneNumber" ? null : PhoneNumber.Create(EmployeeBuilder.DefaultPhoneNumber).Value;
        var joinedOn = parameterName == "joinedOn" ? null : JoinedOn.Create(EmployeeBuilder.DefaultJoinedOn).Value;

        var act = () => EmployeeAggregate.Register(EmployeeBuilder.DefaultId, name!, email!, phoneNumber!, joinedOn!);

        act.Should().Throw<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Register_EmptyIdAndNullValueObject_ReportsIdFirst()
    {
        var act = () => EmployeeAggregate.Register(default, null!, null!, null!, null!);

        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    // ---- Deactivate ----

    [Fact]
    public void Deactivate_ActiveEmployee_BecomesInactive()
    {
        var employee = new EmployeeBuilder().Build();

        employee.Deactivate();

        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
    }

    [Fact]
    public void Deactivate_ActiveEmployee_DoesNotChangeOtherPropertiesOrRaiseEvents()
    {
        var builder = new EmployeeBuilder();
        var employee = builder.Build();
        employee.ClearDomainEvents();

        employee.Deactivate();

        employee.Id.Should().Be(EmployeeBuilder.DefaultId);
        employee.Name.Value.Should().Be(EmployeeBuilder.DefaultName);
        employee.Email.Value.Should().Be(builder.EmailInput);
        employee.NormalizedEmail.Should().Be(builder.EmailInput.ToLowerInvariant());
        employee.PhoneNumber.Value.Should().Be(EmployeeBuilder.DefaultPhoneNumber);
        employee.JoinedOn.Value.Should().Be(new DateOnly(2020, 3, 2));
        employee.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_CalledTwice_StaysInactiveWithoutEvents()
    {
        // 같은 상태로의 전이는 멱등(변경 없음)이다(S03-T01 결정 유지).
        var employee = new EmployeeBuilder().Build();
        employee.Deactivate();
        employee.ClearDomainEvents();

        var act = employee.Deactivate;

        act.Should().NotThrow();
        employee.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
        employee.DomainEvents.Should().BeEmpty();
    }

    // ---- 빌더 ----

    [Fact]
    public void Builder_DefaultEmail_IsUniquePerBuilder()
    {
        var first = new EmployeeBuilder().Build();
        var second = new EmployeeBuilder().Build();

        first.NormalizedEmail.Should().NotBe(second.NormalizedEmail);
    }
}
