using System.Reflection;
using System.Text;
using EmergencyHub.BuildingBlocks.Api.Errors;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.Controllers;
using EmergencyHub.Employee.Api.Employees.Import;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyHub.Employee.Api.UnitTests.Controllers;

// S06-T05 완료 조건 ② · ③ · ⑥(ADR-0016 얇은 Controller, ADR-0025 규칙 예외): ISender만 받고 입력 → Command, Result → 응답만 한다.
// 성공은 201 + { count, ids }이고 Location이 없다(CreatedResult가 아닌 ObjectResult). 실패는 ToProblemResult(ErrorProblemResult).
// 액션 선언: POST api/employee, [Consumes] 4종, 크기 한도 3개(1 MiB), 폼 값 공급자 제외.
[Trait("FR", "PRD-002/FR-05")]
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

    private static EmployeeImportPayload Payload() =>
        new(EmployeeImportFormat.Csv, EmployeeImportSources.Body, Encoding.UTF8.GetBytes("홍길동,hong@example.com,010-1234-5678,2020-01-02"));

    private EmployeeController CreateController() => new(_sender);
}
