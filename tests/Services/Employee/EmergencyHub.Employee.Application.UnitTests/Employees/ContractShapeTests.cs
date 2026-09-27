using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees;

// T02(Read Repository 프로젝션) · T04(Controller 매핑)가 기대는 계약 모양을 고정한다.
public sealed class ContractShapeTests
{
    [Fact]
    public void RegisterEmployeeCommand_IsRecordCommandReturningEmployeeIdWithNullableStatus()
    {
        typeof(ICommand<EmployeeId>).IsAssignableFrom(typeof(RegisterEmployeeCommand)).Should().BeTrue();
        PositionalParameters(typeof(RegisterEmployeeCommand)).Should().Equal(
            ("DisplayName", typeof(string)),
            ("Email", typeof(string)),
            ("EmployeeStatus", typeof(EmployeeStatus?)));
    }

    [Fact]
    public void GetEmployeeByIdQuery_IsQueryOfEmployeeResponseByGuid()
    {
        typeof(IQuery<EmployeeResponse>).IsAssignableFrom(typeof(GetEmployeeByIdQuery)).Should().BeTrue();
        PositionalParameters(typeof(GetEmployeeByIdQuery)).Should().Equal(("Id", typeof(Guid)));
    }

    [Fact]
    public void EmployeeResponse_HasDocumentedMembersInOrder()
    {
        // 인계 메모: EmployeeResponse(id, displayName, email, employeeStatus, createdAt, updatedAt). 코드는 정수 enum으로 직렬화한다.
        PositionalParameters(typeof(EmployeeResponse)).Should().Equal(
            ("Id", typeof(Guid)),
            ("DisplayName", typeof(string)),
            ("Email", typeof(string)),
            ("EmployeeStatus", typeof(EmployeeStatus)),
            ("CreatedAt", typeof(DateTimeOffset)),
            ("UpdatedAt", typeof(DateTimeOffset)));
    }

    [Fact]
    public void EmployeeReadRepository_InheritsReadMarkerOnly()
    {
        typeof(IReadRepository).IsAssignableFrom(typeof(IEmployeeReadRepository)).Should().BeTrue();
        typeof(BuildingBlocks.Domain.Repositories.IRepository).IsAssignableFrom(typeof(IEmployeeReadRepository)).Should().BeFalse();
    }

    [Fact]
    public void ApplicationAssemblyMarker_PointsToApplicationAssembly()
    {
        EmployeeApplicationAssembly.Assembly.Should().BeSameAs(typeof(RegisterEmployeeCommand).Assembly);
    }

    private static IReadOnlyList<(string Name, Type Type)> PositionalParameters(Type recordType) =>
        [.. recordType.GetConstructors().Single().GetParameters().Select(parameter => (parameter.Name!, parameter.ParameterType))];
}
