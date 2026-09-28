using System.Reflection;
using System.Text;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.Controllers;
using EmergencyHub.Employee.Api.Employees.Import;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.UnitTests.Controllers;

// S06-T05 완료 조건 ② · ③ · ⑥(ADR-0016 얇은 Controller, ADR-0025 규칙 예외): ISender만 받고 입력 → Command, Result → 응답만 한다.
// 성공은 201 + { count, ids }이고 Location이 없다(CreatedResult가 아닌 ObjectResult). 실패는 ToProblemResult(ErrorProblemResult).
// 액션 선언: POST api/employee, [Consumes] 4종, 크기 한도 3개(1 MiB), 폼 값 공급자 제외.
[Trait("FR", "PRD-002/FR-05")]
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-08")]
[Trait("FR", "PRD-002/FR-09")]
[Trait("NFR", "PRD-002/NFR-01")]
public sealed class EmployeeControllerTests
{
    private static readonly Guid FirstId = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");
    private static readonly Guid SecondId = Guid.Parse("0192a1b3-0000-7000-8000-000000000002");

    private static readonly MethodInfo RegisterAction = typeof(EmployeeController).GetMethod(nameof(EmployeeController.RegisterAsync))!;

    private readonly ISender _sender = Substitute.For<ISender>();

    // ---- 성공 ----

    [Fact]
    public async Task RegisterAsync_CommandSucceeds_Returns201WithBodyAndNoLocation()
    {
        var response = new RegisterEmployeesResponse(2, [FirstId, SecondId]);
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await CreateController().RegisterAsync(Payload(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<ObjectResult>("CreatedResult · CreatedAtRouteResult는 Location을 쓴다(ADR-0025: Location 없음)").Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task RegisterAsync_Payload_SendsCommandWithSameFormatSourcesContentAndToken()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new RegisterEmployeesResponse(0, [])));
        using var cancellation = new CancellationTokenSource();
        var payload = new EmployeeImportPayload(EmployeeImportFormat.Json, EmployeeImportSources.File | EmployeeImportSources.Data, Encoding.UTF8.GetBytes("[{}]"));

        await CreateController().RegisterAsync(payload, cancellation.Token);

        await _sender.Received(1).SendAsync(
            Arg.Is<RegisterEmployeesCommand>(command =>
                command.Format == payload.Format && command.Sources == payload.Sources && command.Content.Equals(payload.Content)),
            cancellation.Token);
    }

    [Fact]
    public void RegisterAction_IsPostOnEmployeeRouteWithFourConsumesTypes()
    {
        typeof(EmployeeController).GetCustomAttribute<RouteAttribute>()!.Template.Should().Be("api/employee");
        typeof(EmployeeController).GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        RegisterAction.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().BeNull();
        RegisterAction.GetCustomAttribute<ConsumesAttribute>()!.ContentTypes.Should().Equal(
            "multipart/form-data", "application/x-www-form-urlencoded", "text/csv", "application/json");
        EmployeeController.RegisterContentTypes.Should().Equal(RegisterAction.GetCustomAttribute<ConsumesAttribute>()!.ContentTypes);
    }

    [Fact]
    public void RegisterAction_SizeLimits_AreOneMebibyteAndFormValueProvidersAreDisabled()
    {
        const int OneMebibyte = 1_048_576;
        var formLimits = RegisterAction.GetCustomAttribute<RequestFormLimitsAttribute>()!;

        EmployeeImportLimits.MaxRequestBodyBytes.Should().Be(OneMebibyte);
        ((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)RegisterAction.GetCustomAttribute<RequestSizeLimitAttribute>()!)
            .MaxRequestBodySize.Should().Be(OneMebibyte);
        formLimits.MultipartBodyLengthLimit.Should().Be(OneMebibyte);
        formLimits.ValueLengthLimit.Should().Be(OneMebibyte);
        RegisterAction.GetCustomAttribute<DisableFormValueProvidersAttribute>().Should().NotBeNull("폼 값 공급자가 본문을 먼저 읽으면 한도 초과가 400이 된다");
    }

    [Fact]
    public void Controller_TakesOnlySender()
    {
        typeof(EmployeeController).GetConstructors().Should().ContainSingle()
            .Which.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(typeof(ISender));
    }

    // ---- 실패 ----

    [Fact]
    public async Task RegisterAsync_ValidationFailed_ReturnsProblemResultWithSameError()
    {
        var validation = ValidationError.Create([FieldError.Create("Rows[3].Email", EmployeeErrors.EmailInvalid)]);
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<RegisterEmployeesResponse>(validation));

        var result = await CreateController().RegisterAsync(Payload(), CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(validation);
    }

    [Fact]
    public async Task RegisterAsync_StoredEmailConflict_ReturnsProblemResultWithSameError()
    {
        var conflict = ConflictError.Create(EmployeeErrors.DuplicateEmail, [ConflictDetail.Create("Rows[1].Email", EmployeeErrors.DuplicateEmail)]);
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<RegisterEmployeesResponse>(conflict));

        var result = await CreateController().RegisterAsync(Payload(), CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(conflict);
    }

    [Fact]
    public async Task RegisterAsync_NullPayload_ThrowsWithoutSending()
    {
        var act = () => CreateController().RegisterAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        await _sender.DidNotReceiveWithAnyArgs().SendAsync<RegisterEmployeesResponse>(default!, TestContext.Current.CancellationToken);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task RegisterAsync_EmptyPayload_IsSentAsIsForValidator()
    {
        // 빈 입력(Sources None · Format Unknown) 판정은 Validator(21028) 몫이다. Controller는 거르지 않는다.
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RegisterEmployeesResponse>(ValidationError.Create([FieldError.Create(string.Empty, EmployeeErrors.ImportInputEmpty)])));
        var payload = new EmployeeImportPayload(EmployeeImportFormat.Unknown, EmployeeImportSources.None, ReadOnlyMemory<byte>.Empty);

        var result = await CreateController().RegisterAsync(payload, CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>();
        await _sender.Received(1).SendAsync(
            Arg.Is<RegisterEmployeesCommand>(command => command.Sources == EmployeeImportSources.None && command.Content.IsEmpty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ZeroRowsSucceeded_Returns201WithEmptyIds()
    {
        _sender.SendAsync(Arg.Any<RegisterEmployeesCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new RegisterEmployeesResponse(0, [])));

        var result = await CreateController().RegisterAsync(Payload(), CancellationToken.None);

        var created = result.Result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeOfType<RegisterEmployeesResponse>().Which.Ids.Should().BeEmpty();
    }

    // ---- 목록 조회(S07-T01, PRD-002 FR-07): 성공 ----

    [Fact]
    public async Task ListAsync_QuerySucceeds_Returns200WithSameResponse()
    {
        var response = new ListEmployeesResponse([], 25, 4, 10);
        _sender.QueryAsync(Arg.Any<ListEmployeesQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await CreateController().ListAsync(4, 10, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task ListAsync_PageAndPageSize_SendsQueryWithSameValuesAndToken()
    {
        _sender.QueryAsync(Arg.Any<ListEmployeesQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new ListEmployeesResponse([], 0, 2, 10)));
        using var cancellation = new CancellationTokenSource();

        await CreateController().ListAsync(2, 10, cancellation.Token);

        await _sender.Received(1).QueryAsync(new ListEmployeesQuery(2, 10), cancellation.Token);
    }

    [Fact]
    public void ListAction_IsGetOnEmployeeRouteWithNullableQueryParameters()
    {
        var action = typeof(EmployeeController).GetMethod(nameof(EmployeeController.ListAsync))!;

        action.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().BeNull();
        var parameters = action.GetParameters();
        parameters.Select(parameter => (parameter.Name, parameter.ParameterType)).Should().Equal(
            ("page", typeof(int?)), ("pageSize", typeof(int?)), ("cancellationToken", typeof(CancellationToken)));
        parameters.Take(2).Should().AllSatisfy(parameter => parameter.GetCustomAttribute<FromQueryAttribute>().Should().NotBeNull());
    }

    // ---- 목록 조회: 실패 ----

    [Fact]
    public async Task ListAsync_ValidationFailed_ReturnsProblemResultWithSameError()
    {
        var validation = ValidationError.Create([FieldError.Create("Page", CommonErrors.InvalidPaging)]);
        _sender.QueryAsync(Arg.Any<ListEmployeesQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<ListEmployeesResponse>(validation));

        var result = await CreateController().ListAsync(0, null, CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(validation);
    }

    // ---- 목록 조회: 엣지 ----

    [Fact]
    public async Task ListAsync_MissingValues_SendsDefaultsFromQuery()
    {
        _sender.QueryAsync(Arg.Any<ListEmployeesQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new ListEmployeesResponse([], 0, 1, 20)));

        await CreateController().ListAsync(null, null, CancellationToken.None);

        await _sender.Received(1).QueryAsync(new ListEmployeesQuery(ListEmployeesQuery.DefaultPage, ListEmployeesQuery.DefaultPageSize), Arg.Any<CancellationToken>());
    }

    // ---- 이름 조회(S07-T02, PRD-002 FR-08): 성공 ----

    [Fact]
    public async Task GetByNameAsync_QuerySucceeds_Returns200WithSameResponse()
    {
        var response = new EmployeeResponse(FirstId, "홍길동", "hong@example.com", "010-1234-5678", new DateOnly(2020, 1, 2));
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await CreateController().GetByNameAsync("홍길동", CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task GetByNameAsync_Name_SendsQueryWithRouteValueAsIsAndToken()
    {
        // 정규화(Trim + NFC)는 Validator · Handler(Name.Create) 몫이라 Controller는 받은 값을 그대로 넘긴다.
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));
        using var cancellation = new CancellationTokenSource();

        await CreateController().GetByNameAsync(" 홍길동 ", cancellation.Token);

        await _sender.Received(1).QueryAsync(new GetEmployeeByNameQuery(" 홍길동 "), cancellation.Token);
    }

    [Fact]
    public void GetByNameAction_IsGetOnNameRouteTemplate()
    {
        var action = typeof(EmployeeController).GetMethod(nameof(EmployeeController.GetByNameAsync))!;

        action.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("{name}");
        action.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType))
            .Should().Equal(("name", typeof(string)), ("cancellationToken", typeof(CancellationToken)));
    }

    // ---- 이름 조회: 실패 ----

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsProblemResultWith22001()
    {
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        var result = await CreateController().GetByNameAsync("없는사람", CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(EmployeeErrors.NotFound);
    }

    [Fact]
    public async Task GetByNameAsync_ValidationFailed_ReturnsProblemResultWithSameError()
    {
        var validation = ValidationError.Create([FieldError.Create("Name", EmployeeErrors.NameRequired)]);
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(validation));

        var result = await CreateController().GetByNameAsync(" ", CancellationToken.None);

        result.Result.Should().BeOfType<ErrorProblemResult>().Which.Error.Should().BeSameAs(validation);
    }

    // ---- 이름 조회: 엣지 ----

    [Fact]
    public async Task GetByNameAsync_NullName_SendsQueryWithNull()
    {
        // 공백만 있는 경로 값은 단순 형식 바인더가 null로 바꾼다. 판정(21007)은 Validator 몫이라 Controller는 그대로 넘긴다.
        _sender.QueryAsync(Arg.Any<GetEmployeeByNameQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<EmployeeResponse>(EmployeeErrors.NotFound));

        await CreateController().GetByNameAsync(null, CancellationToken.None);

        await _sender.Received(1).QueryAsync(new GetEmployeeByNameQuery(null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetByNameAction_DeclaresOkBadRequestAndNotFoundProblemResponses()
    {
        var action = typeof(EmployeeController).GetMethod(nameof(EmployeeController.GetByNameAsync))!;

        action.GetCustomAttributes<ProducesResponseTypeAttribute>().Select(attribute => attribute.StatusCode)
            .Should().BeEquivalentTo([StatusCodes.Status200OK, StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound]);
    }

    private static EmployeeImportPayload Payload() =>
        new(EmployeeImportFormat.Csv, EmployeeImportSources.Body, Encoding.UTF8.GetBytes("홍길동,hong@example.com,010-1234-5678,2020-01-02"));

    private EmployeeController CreateController() => new(_sender);
}
