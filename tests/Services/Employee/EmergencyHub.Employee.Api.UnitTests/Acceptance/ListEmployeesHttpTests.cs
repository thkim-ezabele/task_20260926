using System.Net;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Api.UnitTests.TestDoubles;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;

namespace EmergencyHub.Employee.Api.UnitTests.Acceptance;

// S07-T01(PRD-002 FR-07, ADR-0025): 운영 등록으로 띄운 TestServer에서 쿼리 문자열 바인딩(int?) · 응답 JSON 이름 · 바인딩 오류 1001을 확인한다.
// ISender만 대역이라 Validator · Handler · DB는 실행하지 않는다(범위 밖 1003 · 페이징 경계의 전 구간은 S07-T03 통합 테스트).
[Trait("FR", "PRD-002/FR-07")]
[Trait("FR", "PRD-002/FR-09")]
public sealed class ListEmployeesHttpTests : IAsyncLifetime
{
    private const string Path = "/api/employee";

    private static readonly Guid EmployeeId = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly List<ListEmployeesQuery> _queries = [];
    private EmployeeApiTestHost _host = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _sender.QueryAsync(Arg.Do<ListEmployeesQuery>(_queries.Add), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ListEmployeesResponse(
                [new EmployeeResponse(EmployeeId, "김이름", "kim@gmail.com", "010-0000-0000", new DateOnly(2000, 1, 1))], 25, 2, 10)));
        _host = await EmployeeApiTestHost.StartAsync(_sender, "Production", CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    // ---- 성공 ----

    [Fact]
    public async Task Get_PageAndPageSize_Returns200WithItemsTotalCountPageAndPageSize()
    {
        using var response = await _host.Client.GetAsync($"{Path}?page=2&pageSize=10", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var root = json.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should().Equal("items", "totalCount", "page", "pageSize");
        root.GetProperty("totalCount").GetInt32().Should().Be(25);
        root.GetProperty("page").GetInt32().Should().Be(2);
        root.GetProperty("pageSize").GetInt32().Should().Be(10);
        var item = root.GetProperty("items").EnumerateArray().Should().ContainSingle().Subject;
        item.EnumerateObject().Select(property => property.Name).Should().Equal("id", "name", "email", "tel", "joined");
        item.GetProperty("id").GetGuid().Should().Be(EmployeeId);
        item.GetProperty("tel").GetString().Should().Be("010-0000-0000");
        item.GetProperty("joined").GetString().Should().Be("2000-01-01");
        _queries.Should().Equal(new ListEmployeesQuery(2, 10));
    }

    [Fact]
    public async Task Get_WithoutQueryString_SendsDefaultPageOneAndPageSizeTwenty()
    {
        using var response = await _host.Client.GetAsync(Path, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _queries.Should().Equal(new ListEmployeesQuery(1, 20));
    }

    // ---- 실패: 숫자가 아닌 값 · int 범위 밖은 바인딩 오류 1001이고 Query를 보내지 않는다 ----

    [Theory]
    [InlineData("page=abc")]
    [InlineData("pageSize=abc")]
    [InlineData("page=2147483648")]
    [InlineData("pageSize=-2147483649")]
    [InlineData("page=1.5")]
    public async Task Get_NonIntegerValue_Returns400WithBindingCode1001(string queryString)
    {
        using var response = await _host.Client.GetAsync($"{Path}?{queryString}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("code").GetInt32().Should().Be(1001);
        _queries.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_QueryValidationFails_Returns400WithFieldCode1003()
    {
        // 검증 데코레이터가 돌려주는 모양(대표 1001 + 필드 1003)을 ProblemDetails로 옮긴다.
        _sender.QueryAsync(Arg.Any<ListEmployeesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ListEmployeesResponse>(ValidationError.Create([FieldError.Create("Page", CommonErrors.InvalidPaging)])));

        using var response = await _host.Client.GetAsync($"{Path}?page=0", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("errors").GetProperty("page")[0].GetProperty("code").GetInt32().Should().Be(1003);
    }

    // ---- 엣지: 범위 밖 정수는 바인딩을 지나 Validator(1003)로 간다 ----

    [Theory]
    [InlineData("page=-1", -1, 20)]
    [InlineData("page=0&pageSize=101", 0, 101)]
    [InlineData("page=2147483647&pageSize=-2147483648", int.MaxValue, int.MinValue)]
    public async Task Get_OutOfRangeInteger_IsSentAsIsForValidator(string queryString, int page, int pageSize)
    {
        using var response = await _host.Client.GetAsync($"{Path}?{queryString}", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "대역 ISender가 성공을 돌려준다");
        _queries.Should().Equal(new ListEmployeesQuery(page, pageSize));
    }

    [Fact]
    public async Task Get_ParameterNamesAreCaseInsensitive()
    {
        using var response = await _host.Client.GetAsync($"{Path}?PAGE=3&PageSize=5", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _queries.Should().Equal(new ListEmployeesQuery(3, 5));
    }
}
