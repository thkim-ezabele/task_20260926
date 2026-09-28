using System.Globalization;
using System.Net;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S07-T03 완료 조건 ①(PRD-002 FR-07): 실제 Api 파이프라인(TestServer) + 컨테이너 DB에서 GET /api/employee의 페이징 경계를 전 구간으로 확인한다
// (바인딩 → 검증 데코레이터 → Handler → Read Repository → JSON). Api.UnitTests는 ISender 대역, Infrastructure.UnitTests는 DI 파이프라인까지라 DB를 거친 HTTP 응답은 여기서만 본다.
// 25건은 ID가 등록 순으로 커지고 입사일이 뒤섞이며 하루에 2명 이상인 날이 있어, 목록 순서(입사일 → ID)가 등록 순서와 다르다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-07")]
public sealed class ListEmployeesPagingTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string Path = "/api/employee";
    private const int Rows = 25;

    // ---- 성공 ----

    [Fact]
    public async Task Get_Page2PageSize10Of25_Returns11thTo20thInJoinedOnThenIdOrder()
    {
        await using var factory = new EmployeeApiFactory(Database);
        var expected = await SeedAsync(factory, Rows);

        using var response = await factory.CreateClient().GetAsync($"{Path}?page=2&pageSize=10", CancellationToken);

        var root = await ShouldBeOkAsync(response, totalCount: Rows, page: 2, pageSize: 10);
        Ids(root).Should().Equal(expected.Skip(10).Take(10).Select(employee => employee.Id), "11 ~ 20번째(입사일 → ID)");
        Ids(root).Should().NotEqual(expected.OrderBy(employee => employee.Index).Skip(10).Take(10).Select(employee => employee.Id), "등록 순서와 다른 데이터로 정렬을 확인한다");
        var first = root.GetProperty("items")[0];
        var seeded = expected[10];
        first.GetProperty("name").GetString().Should().Be(seeded.Name);
        first.GetProperty("email").GetString().Should().Be(seeded.Email);
        first.GetProperty("tel").GetString().Should().Be(EmployeeBuilder.DefaultPhoneNumber);
        first.GetProperty("joined").GetString().Should().Be(seeded.JoinedOn);
    }

    [Theory]
    [InlineData("", 1, 20, 0, 20)]
    [InlineData("?page=3&pageSize=10", 3, 10, 20, 5)]
    [InlineData("?page=1&pageSize=100", 1, 100, 0, 25)]
    public async Task Get_DefaultLastPartialOrMaxPageSize_ReturnsExpectedSlice(string query, int page, int pageSize, int skip, int count)
    {
        await using var factory = new EmployeeApiFactory(Database);
        var expected = await SeedAsync(factory, Rows);

        using var response = await factory.CreateClient().GetAsync(Path + query, CancellationToken);

        var root = await ShouldBeOkAsync(response, totalCount: Rows, page: page, pageSize: pageSize);
        Ids(root).Should().Equal(expected.Skip(skip).Take(count).Select(employee => employee.Id));
    }

    // ---- 실패: 범위 밖 정수는 400 · 대표 1001 + 필드 1003, 정수가 아니거나 int 범위 밖이면 바인딩 1001 ----

    [Theory]
    [InlineData("page=0", "page")]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("page=100001", "page")]
    [InlineData("pageSize=0", "pageSize")]
    public async Task Get_OutOfRangePaging_Returns400WithFieldCode1003(string query, string field)
    {
        await using var factory = new EmployeeApiFactory(Database);
        await SeedAsync(factory, 1);

        using var response = await factory.CreateClient().GetAsync($"{Path}?{query}", CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code, CommonErrors.ValidationFailed.Message, Path, CancellationToken);
        problem.FieldCodes().Should().Equal((field, CommonErrors.InvalidPaging.Code));
    }

    [Fact]
    public async Task Get_Page0AndPageSize101_Returns400With1003OnBothFields()
    {
        await using var factory = new EmployeeApiFactory(Database);

        using var response = await factory.CreateClient().GetAsync($"{Path}?page=0&pageSize=101", CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code, CommonErrors.ValidationFailed.Message, Path, CancellationToken);
        problem.FieldCodes().Should().BeEquivalentTo([("page", CommonErrors.InvalidPaging.Code), ("pageSize", CommonErrors.InvalidPaging.Code)]);
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("pageSize=abc")]
    [InlineData("page=2147483648")]
    public async Task Get_NonIntegerPaging_Returns400WithBindingCode1001(string query)
    {
        await using var factory = new EmployeeApiFactory(Database);

        using var response = await factory.CreateClient().GetAsync($"{Path}?{query}", CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code, CommonErrors.ValidationFailed.Message, Path, CancellationToken);
        problem.FieldCodes().Should().ContainSingle().Which.Code.Should().Be(CommonErrors.ValidationFailed.Code, "바인딩 오류 필드 코드도 1001(ADR-0016)");
    }

    // ---- 엣지: 마지막 쪽을 넘으면 빈 items · 200 · 올바른 totalCount, 빈 테이블 ----

    [Theory]
    [InlineData(4, 10)]
    [InlineData(100000, 100)]
    public async Task Get_PageBeyondLast_Returns200WithEmptyItemsAndTotalCount(int page, int pageSize)
    {
        await using var factory = new EmployeeApiFactory(Database);
        await SeedAsync(factory, Rows);

        using var response = await factory.CreateClient().GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"{Path}?page={page}&pageSize={pageSize}"), CancellationToken);

        var root = await ShouldBeOkAsync(response, totalCount: Rows, page: page, pageSize: pageSize);
        root.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_EmptyTable_Returns200WithEmptyItemsAndZeroTotalCount()
    {
        await using var factory = new EmployeeApiFactory(Database);

        using var response = await factory.CreateClient().GetAsync(Path, CancellationToken);

        var root = await ShouldBeOkAsync(response, totalCount: 0, page: 1, pageSize: 20);
        root.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    private static async Task<JsonElement> ShouldBeOkAsync(HttpResponseMessage response, int totalCount, int page, int pageSize)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var root = document.RootElement.Clone();
        root.EnumerateObject().Select(property => property.Name).Should().Equal("items", "totalCount", "page", "pageSize");
        root.GetProperty("totalCount").GetInt32().Should().Be(totalCount);
        root.GetProperty("page").GetInt32().Should().Be(page);
        root.GetProperty("pageSize").GetInt32().Should().Be(pageSize);
        return root;
    }

    private static List<Guid> Ids(JsonElement root) => [.. root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())];

    // 등록 순 i(0부터)의 ID는 i가 커질수록 커지고(마지막 12자리만 다름 → uuid 바이트 순 = i 순), 입사일은 (i × 7) % 13일 뒤라 하루에 1 ~ 2명이다.
    // 반환값은 목록 순서(입사일 → ID)로 정렬한 기대값이다.
    private static async Task<List<Seeded>> SeedAsync(EmployeeApiFactory factory, int count)
    {
        var seeded = Enumerable.Range(0, count)
            .Select(index => new Seeded(
                index,
                Guid.Parse(string.Create(CultureInfo.InvariantCulture, $"0190b000-0000-7000-8000-{index:x12}")),
                string.Create(CultureInfo.InvariantCulture, $"페이지{index}"),
                string.Create(CultureInfo.InvariantCulture, $"paging{index}@example.com"),
                new DateOnly(2020, 1, 1).AddDays(index * 7 % 13).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ToList();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IEmployeeRepository>().AddRange(seeded.Select(employee => new EmployeeBuilder()
                .WithId(new EmployeeId(employee.Id))
                .WithName(employee.Name)
                .WithEmail(employee.Email)
                .WithJoinedOn(employee.JoinedOn)
                .Build()));
            (await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken)).IsSuccess.Should().BeTrue();
        }

        return [.. seeded.OrderBy(employee => employee.JoinedOn, StringComparer.Ordinal).ThenBy(employee => employee.Index)];
    }

    private sealed record Seeded(int Index, Guid Id, string Name, string Email, string JoinedOn);
}
