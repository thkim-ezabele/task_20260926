using System.Net;
using System.Text;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;

namespace EmergencyHub.Employee.IntegrationTests.Http;

// S07-T03 완료 조건 ②(PRD-002 FR-08): 실제 Api 파이프라인(TestServer) + 컨테이너 DB에서 GET /api/employee/{name}을 전 구간으로 확인한다
// (라우트 디코딩 → Validator(Name.Create: Trim + NFC) → Handler → Read Repository → JSON). Api.UnitTests는 ISender 대역이라 NFC 정규화 · 동명이인 선택 · 404는 여기서만 DB와 함께 본다.
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-08")]
public sealed class GetEmployeeByNameLookupTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    private const string Name = "홍길동";
    private const string Template = "/api/employee/{name}";

    // ---- 성공 ----

    [Fact]
    public async Task Get_PercentEncodedKoreanName_Returns200WithStoredContact()
    {
        await using var factory = new EmployeeApiFactory(Database);
        var employee = new EmployeeBuilder().WithName(Name).WithEmail("Hong.GilDong@Example.com").WithPhoneNumber("010-2222-3333").WithJoinedOn("2021-04-05").Build();
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        using var response = await factory.CreateClient().GetAsync(NamePath(Name), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var root = json.RootElement;
        root.EnumerateObject().Select(property => property.Name).Should().Equal("id", "name", "email", "tel", "joined");
        root.GetProperty("id").GetGuid().Should().Be(employee.Id.Value);
        root.GetProperty("name").GetString().Should().Be(Name);
        root.GetProperty("email").GetString().Should().Be("Hong.GilDong@Example.com", "입력 표기 그대로");
        root.GetProperty("tel").GetString().Should().Be("010-2222-3333");
        root.GetProperty("joined").GetString().Should().Be("2021-04-05");
    }

    [Fact]
    public async Task PostCsvThenGetByName_Returns200WithCreatedIdAndInputNotationEmail()
    {
        await using var factory = new EmployeeApiFactory(Database);
        using var client = factory.CreateClient();
        using var content = ImportContent.Raw(Encoding.UTF8.GetBytes($"{Name},Hong.Round@Example.COM,010-4444-5555,2020-02-02\n"), "text/csv");
        using var created = await client.PostAsync(ImportContent.RegisterUri, content, CancellationToken);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync(CancellationToken));
        var id = createdJson.RootElement.GetProperty("ids").EnumerateArray().Should().ContainSingle().Which.GetGuid();

        using var response = await client.GetAsync(NamePath(Name), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("id").GetGuid().Should().Be(id, "등록 응답의 ID로 조회된다(POST → GET 왕복)");
        json.RootElement.GetProperty("email").GetString().Should().Be("Hong.Round@Example.COM", "정규화 값이 아니라 입력 표기");
        json.RootElement.GetProperty("tel").GetString().Should().Be("010-4444-5555");
        json.RootElement.GetProperty("joined").GetString().Should().Be("2020-02-02");
    }

    [Fact]
    public async Task Get_NamesakeOfThree_ReturnsEarliestJoinedEvenWhenRegisteredLastWithLargestId()
    {
        await using var factory = new EmployeeApiFactory(Database);
        // 등록 순(= ID 순) 1 · 2 · 3번째의 입사일이 2022 · 2021 · 2019라 가장 빠른 사람은 마지막 등록 · 가장 큰 ID다.
        var ids = new[] { "0190c000-0000-7000-8000-000000000001", "0190c000-0000-7000-8000-000000000002", "0190c000-0000-7000-8000-000000000003" };
        var joined = new[] { "2022-01-01", "2021-01-01", "2019-01-01" };
        for (var index = 0; index < ids.Length; index++)
        {
            var employee = new EmployeeBuilder().WithId(new EmployeeId(Guid.Parse(ids[index]))).WithName(Name).WithJoinedOn(joined[index]).Build();
            (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        }

        using var response = await factory.CreateClient().GetAsync(NamePath(Name), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("id").GetGuid().Should().Be(Guid.Parse(ids[2]));
        json.RootElement.GetProperty("joined").GetString().Should().Be("2019-01-01");
    }

    // ---- 실패 ----

    [Fact]
    public async Task Get_MissingName_Returns404With22001AndTemplateInstance()
    {
        await using var factory = new EmployeeApiFactory(Database);
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().WithName(Name).Build(), CancellationToken)).IsSuccess.Should().BeTrue();

        using var response = await factory.CreateClient().GetAsync(NamePath("홍길순"), CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, EmployeeErrors.NotFound.Code, EmployeeErrors.NotFound.Message, Template, CancellationToken);
    }

    [Theory]
    [InlineData("%20")]
    [InlineData("%20%20%09")]
    public async Task Get_BlankName_Returns400With21007OnName(string encodedBlank)
    {
        await using var factory = new EmployeeApiFactory(Database);

        using var response = await factory.CreateClient().GetAsync("/api/employee/" + encodedBlank, CancellationToken);

        var problem = await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code, CommonErrors.ValidationFailed.Message, Template, CancellationToken);
        problem.FieldCodes().Should().Equal(("name", EmployeeErrors.NameRequired.Code));
    }

    // ---- 엣지: NFD 요청 · 앞뒤 공백은 정규화해 찾고, 대소문자는 구분한다 · 같은 입사일이면 등록 순(ID) ----

    [Fact]
    public async Task Get_NfdEncodedName_FindsNfcStoredEmployee()
    {
        await using var factory = new EmployeeApiFactory(Database);
        var employee = new EmployeeBuilder().WithName(Name).Build();
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        var nfd = Name.Normalize(NormalizationForm.FormD);
        nfd.Should().NotBe(Name, "NFD는 한글 음절을 자모로 나눈다");

        using var response = await factory.CreateClient().GetAsync(NamePath(nfd), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("id").GetGuid().Should().Be(employee.Id.Value);
        json.RootElement.GetProperty("name").GetString().Should().Be(Name, "저장 값(NFC)을 돌려준다");
    }

    [Fact]
    public async Task Get_NameWithSurroundingSpaces_FindsTrimmedName()
    {
        await using var factory = new EmployeeApiFactory(Database);
        var employee = new EmployeeBuilder().WithName(Name).Build();
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();

        using var response = await factory.CreateClient().GetAsync(NamePath("  " + Name + " "), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("id").GetGuid().Should().Be(employee.Id.Value);
    }

    [Fact]
    public async Task Get_CaseDifferentName_Returns404()
    {
        await using var factory = new EmployeeApiFactory(Database);
        (await EmployeeCommits.AddAndCommitAsync(factory.Services, new EmployeeBuilder().WithName("Hong Gildong").Build(), CancellationToken)).IsSuccess.Should().BeTrue();

        using var response = await factory.CreateClient().GetAsync(NamePath("hong gildong"), CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, EmployeeErrors.NotFound.Code, EmployeeErrors.NotFound.Message, Template, CancellationToken);
    }

    [Fact]
    public async Task Get_NamesakeWithSameJoinedOn_ReturnsSmallerIdRegardlessOfInsertOrder()
    {
        await using var factory = new EmployeeApiFactory(Database);
        var larger = Guid.Parse("0190c000-0000-7000-8000-0000000000ff");
        var smaller = Guid.Parse("0190c000-0000-7000-8000-000000000010");
        foreach (var id in new[] { larger, smaller })
        {
            var employee = new EmployeeBuilder().WithId(new EmployeeId(id)).WithName(Name).WithJoinedOn("2020-05-05").Build();
            (await EmployeeCommits.AddAndCommitAsync(factory.Services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        }

        using var response = await factory.CreateClient().GetAsync(NamePath(Name), CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        json.RootElement.GetProperty("id").GetGuid().Should().Be(smaller);
    }

    private static Uri NamePath(string name) => new("/api/employee/" + Uri.EscapeDataString(name), UriKind.Relative);
}
