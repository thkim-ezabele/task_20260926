using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.Controllers;
using EmergencyHub.Employee.Api.Employees;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.UnitTests.Controllers;

// ADR-0016: Controller는 ISender만 받고 요청 → Command / Query 변환, Result → 응답 변환만 한다.
// 성공은 201 + Location(GET 경로와 같은 라우트 이름) + { id } / 200, 실패는 BuildingBlocks.Api(ToProblemResult)가 ProblemDetails로 바꾼다.
[Trait("FR", "PRD-001/FR-08")]
[Trait("FR", "PRD-001/FR-07")]
public sealed class EmployeesControllerTests
{
    private static readonly EmployeeId NewId = new(Guid.Parse("0192a1b3-0000-7000-8000-000000000001"));

    private readonly ISender _sender = Substitute.For<ISender>();

    // ---- 등록: 성공 ----

    [Fact]
    public async Task RegisterAsync_CommandSucceeds_Returns201AtGetRouteWithIdBody()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(NewId));
        var controller = CreateController();

        var response = await controller.RegisterAsync(new RegisterEmployeeRequest("홍길동", "hong@example.com", EmployeeStatus.Active), CancellationToken.None);

        var created = response.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.RouteName.Should().Be(EmployeesController.GetByIdRouteName);
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(NewId.Value);
        created.Value.Should().Be(new RegisterEmployeeResponse(NewId.Value));
    }

    [Fact]
    public async Task RegisterAsync_Request_SendsCommandWithSameValuesAndToken()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(NewId));
        using var cancellation = new CancellationTokenSource();
        var controller = CreateController();

        await controller.RegisterAsync(new RegisterEmployeeRequest(" 홍길동 ", " Hong@Example.com ", EmployeeStatus.Inactive), cancellation.Token);

        // 정규화(Trim · 소문자)는 Aggregate가 한다. Controller는 원본을 그대로 넘긴다.
        await _sender.Received(1).SendAsync(
            new RegisterEmployeeCommand(" 홍길동 ", " Hong@Example.com ", EmployeeStatus.Inactive),
            cancellation.Token);
    }

    // ---- 등록: 실패 ----

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsProblemResultWith23001()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeId>(EmployeeErrors.DuplicateEmail));
        var controller = CreateController();

        var response = await controller.RegisterAsync(new RegisterEmployeeRequest("홍길동", "hong@example.com", EmployeeStatus.Active), CancellationToken.None);

        response.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
    }

    [Fact]
    public async Task RegisterAsync_ValidationFailed_ReturnsProblemResultWithValidationError()
    {
        var validation = ValidationError.Create([FieldError.Create("DisplayName", EmployeeErrors.DisplayNameRequired)]);
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeId>(validation));
        var controller = CreateController();

        var response = await controller.RegisterAsync(new RegisterEmployeeRequest(null, null, null), CancellationToken.None);

        response.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(validation);
    }

    [Fact]
    public async Task RegisterAsync_NullRequest_ThrowsArgumentNullException()
    {
        var controller = CreateController();

        var act = () => controller.RegisterAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    // ---- 등록: 엣지 ----

    [Fact]
    public async Task RegisterAsync_NullFields_SendsEmptyStringsAndNullStatus()
    {
        // 누락 · null 문자열은 빈 문자열로 넘겨 Validator가 21001 · 21003으로, 상태 누락은 null 그대로 21006으로 판정한다(S03-T01 인계).
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeId>(EmployeeErrors.DisplayNameRequired));
        var controller = CreateController();

        await controller.RegisterAsync(new RegisterEmployeeRequest(null, null, null), CancellationToken.None);

        await _sender.Received(1).SendAsync(new RegisterEmployeeCommand(string.Empty, string.Empty, null), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)99)]
    public async Task RegisterAsync_UndefinedStatus_PassesValueUnchangedForValidator(short status)
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeId>(CommonErrors.ValidationFailed));
        var controller = CreateController();

        await controller.RegisterAsync(new RegisterEmployeeRequest("홍길동", "hong@example.com", (EmployeeStatus)status), CancellationToken.None);

        // 정의되지 않은 값(0 · 99)은 Controller가 거르지 않고 Validator가 1002로 거부한다.
        await _sender.Received(1).SendAsync(
            Arg.Is<RegisterEmployeeCommand>(command => command.EmployeeStatus == (EmployeeStatus)status),
            Arg.Any<CancellationToken>());
    }

    // ---- 조회: 성공 ----

    [Fact]
    public async Task GetByIdAsync_Found_Returns200WithResponse()
    {
        var employee = new EmployeeResponse(
            NewId.Value,
            "홍길동",
            "hong@example.com",
            EmployeeStatus.Active,
            new DateTimeOffset(2026, 9, 27, 5, 3, 12, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 27, 5, 3, 12, TimeSpan.Zero));
        _sender.QueryAsync(Arg.Any<GetEmployeeByIdQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(employee));
        using var cancellation = new CancellationTokenSource();
        var controller = CreateController();

        var response = await controller.GetByIdAsync(NewId.Value, cancellation.Token);

        response.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(employee);
        await _sender.Received(1).QueryAsync(new GetEmployeeByIdQuery(NewId.Value), cancellation.Token);
    }

    // ---- 조회: 실패 ----

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsProblemResultWith22001()
    {
        _sender.QueryAsync(Arg.Any<GetEmployeeByIdQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));
        var controller = CreateController();

        var response = await controller.GetByIdAsync(NewId.Value, CancellationToken.None);

        var problem = response.Result.Should().BeOfType<ErrorProblemResult>().Subject;
        problem.Error.Should().BeSameAs(EmployeeErrors.NotFound);
        problem.Error.Code.Should().Be(22001);
    }

    // ---- 조회: 엣지 ----

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_StillSendsQuery()
    {
        // 경로 제약이 없으므로(S03 계획 결정) 빈 Guid도 Query로 보내고 없으면 22001이다.
        _sender.QueryAsync(Arg.Any<GetEmployeeByIdQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));
        var controller = CreateController();

        var response = await controller.GetByIdAsync(Guid.Empty, CancellationToken.None);

        response.Result.Should().BeOfType<ErrorProblemResult>();
        await _sender.Received(1).QueryAsync(new GetEmployeeByIdQuery(Guid.Empty), Arg.Any<CancellationToken>());
    }

    // ---- 형태 ----

    [Fact]
    public void Constructor_DependsOnlyOnSender()
    {
        var constructors = typeof(EmployeesController).GetConstructors();

        constructors.Should().ContainSingle();
        constructors[0].GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(typeof(ISender));
    }

    [Fact]
    public void Routes_AreApiV1EmployeesWithoutIdConstraint()
    {
        typeof(EmployeesController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Should().ContainSingle().Which.As<RouteAttribute>().Template.Should().Be("api/v1/employees");
        typeof(EmployeesController).GetCustomAttributes(typeof(ApiControllerAttribute), inherit: false).Should().ContainSingle();

        var get = typeof(EmployeesController).GetMethod(nameof(EmployeesController.GetByIdAsync))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).Should().ContainSingle().Which.As<HttpGetAttribute>();
        get.Template.Should().Be("{id}", "경로 제약(:guid)을 두지 않는다(S03 계획 결정). 형식 오류는 바인딩 1001");
        get.Name.Should().Be(EmployeesController.GetByIdRouteName);

        typeof(EmployeesController).GetMethod(nameof(EmployeesController.RegisterAsync))!
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Should().ContainSingle().Which.As<HttpPostAttribute>().Template.Should().BeNull();
    }

    private EmployeesController CreateController() => new(_sender);
}
