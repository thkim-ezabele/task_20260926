using EmergencyHub.Employee.Application.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees;

// S07-T01(PRD-002 FR-07 · FR-08): 조회 응답 항목은 id · name · email · tel · joined다. Read Repository 프로젝션(PhoneNumber · JoinedOn)을 API 이름으로 옮긴다.
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-08")]
public sealed class EmployeeResponseTests
{
    // ---- 성공 ----

    [Fact]
    public void From_Contact_CopiesEveryFieldToApiNames()
    {
        var id = Guid.NewGuid();
        var contact = new EmployeeContactResponse(id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2));

        var response = EmployeeResponse.From(contact);

        response.Should().Be(new EmployeeResponse(id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2)));
    }

    [Fact]
    public void Properties_AreIdNameEmailTelJoinedInOrder()
    {
        // camelCase JSON 이름이 tel · joined가 되려면 속성 이름이 Tel · Joined여야 한다(PRD-002 FR-07 항목 이름).
        typeof(EmployeeResponse).GetConstructors().Should().ContainSingle()
            .Which.GetParameters().Select(parameter => parameter.Name).Should().Equal("Id", "Name", "Email", "Tel", "Joined");
    }

    // ---- 실패 ----

    [Fact]
    public void From_Null_Throws()
    {
        var act = () => EmployeeResponse.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void From_EmailInputNotation_IsKeptWithoutNormalization()
    {
        // 조회 응답의 이메일은 입력 표기(정규화 값 아님, S05-T06 프로젝션)다.
        var contact = new EmployeeContactResponse(Guid.NewGuid(), "김", "MiXeD@Example.COM", "01012345678", DateOnly.MinValue);

        EmployeeResponse.From(contact).Email.Should().Be("MiXeD@Example.COM");
    }
}
