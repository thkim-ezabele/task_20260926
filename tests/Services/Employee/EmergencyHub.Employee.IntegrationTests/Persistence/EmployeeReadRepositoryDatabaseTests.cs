using System.Text;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.IntegrationTests.Fixtures;
using EmergencyHub.Employee.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyHub.Employee.IntegrationTests.Persistence;

// S05-T06 완료 조건 ②(PRD-002 FR-07 · FR-08): Read Repository의 목록(joined_on → id, Skip / Take) · 개수 · 이름 단건(동명이인이면 joined_on → id 순 첫 1명)을
// 읽기 연결(default_transaction_read_only=on)에서 확인한다. 등록 순 = id 순은 운영 IIdGenerator(UUID v7) 생성 순서로 만든다(EmployeeSchemaTests UUID v7 정렬).
[Collection(EmployeeDatabaseCollectionDefinition.Name)]
[Trait("FR", "PRD-002/FR-07")]
public sealed class EmployeeReadRepositoryDatabaseTests(EmployeeDatabaseFixture database) : EmployeeDatabaseTest(database)
{
    // ---- 성공 ----

    [Fact]
    public async Task ListOrderedByJoinedOnAsync_TwentyFiveEmployeesSecondPageOfTen_ReturnsEleventhToTwentiethByJoinedOnThenRegistration()
    {
        // 입사일 5종 × 5명(같은 입사일은 등록 순). 저장 순서를 섞어도 결과는 (joined_on, id) 순이다.
        await using var services = Database.CreateServices();
        var expected = await SeedInRegistrationOrderAsync(services, Enumerable.Range(0, 25).Select(i => $"20{10 + (i % 5):00}-01-01").ToList());
        await using var scope = services.CreateAsyncScope();

        var page = await ReadRepository(scope).ListOrderedByJoinedOnAsync(10, 10, CancellationToken);

        var ordered = expected.OrderBy(item => item.JoinedOn).ThenBy(item => expected.IndexOf(item)).ToList();
        page.Should().Equal(ordered.Skip(10).Take(10));
    }

    [Fact]
    public async Task CountAsync_TwentyFiveEmployees_ReturnsTotalIncludingInactive()
    {
        await using var services = Database.CreateServices();
        await SeedInRegistrationOrderAsync(services, Enumerable.Repeat(EmployeeBuilder.DefaultJoinedOn, 24).ToList());
        (await EmployeeCommits.AddAndCommitAsync(services, new EmployeeBuilder().WithStatus(EmployeeStatus.Inactive).Build(), CancellationToken)).IsSuccess.Should().BeTrue();
        await using var scope = services.CreateAsyncScope();

        var count = await ReadRepository(scope).CountAsync(CancellationToken);

        count.Should().Be(25);
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_ThreeNamesakes_ReturnsEarliestJoinedOnWithContactFields()
    {
        await using var services = Database.CreateServices();
        var earliest = new EmployeeBuilder().WithName("홍길동").WithEmail("Hong.Early@Example.com").WithPhoneNumber("02-123-4567").WithJoinedOn("2015-05-05").Build();
        await CommitAllAsync(
            services,
            new EmployeeBuilder().WithName("홍길동").WithJoinedOn("2020-01-01").Build(),
            earliest,
            new EmployeeBuilder().WithName("홍길동").WithJoinedOn("2018-01-01").Build(),
            new EmployeeBuilder().WithName("김철수").WithJoinedOn("2000-01-01").Build());
        await using var scope = services.CreateAsyncScope();

        var found = await ReadRepository(scope).FindFirstByNameAsync(Name.Create("홍길동").Value, CancellationToken);

        found.Should().Be(new EmployeeContactResponse(earliest.Id.Value, "홍길동", "Hong.Early@Example.com", "02-123-4567", new DateOnly(2015, 5, 5)));
    }

    // ---- 실패 ----

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_UnknownOrCaseDifferentName_ReturnsNull()
    {
        // 이름은 정확히 일치(대소문자 구분)해야 한다. 없는 이름 → null(404 판정은 S07 Handler).
        await using var services = Database.CreateServices();
        await CommitAllAsync(services, new EmployeeBuilder().WithName("Hong Gildong").Build());
        await using var scope = services.CreateAsyncScope();
        var repository = ReadRepository(scope);

        var results = new[]
        {
            await repository.FindFirstByNameAsync(Name.Create("없는 이름").Value, CancellationToken),
            await repository.FindFirstByNameAsync(Name.Create("hong gildong").Value, CancellationToken),
            await repository.FindFirstByNameAsync(Name.Create("Hong").Value, CancellationToken),
        };

        results.Should().AllSatisfy(result => result.Should().BeNull());
    }

    [Fact]
    public async Task ListOrderedByJoinedOnAsync_PageBeyondLast_ReturnsEmpty()
    {
        await using var services = Database.CreateServices();
        await SeedInRegistrationOrderAsync(services, Enumerable.Repeat(EmployeeBuilder.DefaultJoinedOn, 3).ToList());
        await using var scope = services.CreateAsyncScope();

        var page = await ReadRepository(scope).ListOrderedByJoinedOnAsync(20, 10, CancellationToken);

        page.Should().BeEmpty();
    }

    // ---- 엣지 ----

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_NamesakesWithSameJoinedOn_ReturnsFirstRegistered()
    {
        // 입사일이 같으면 등록 순(id, UUID v7 생성 순)으로 첫 1명이다. 저장은 역순으로 한다.
        await using var services = Database.CreateServices();
        var registered = await SeedInRegistrationOrderAsync(services, Enumerable.Repeat("2020-03-02", 3).ToList(), name: "동명이", reverseCommit: true);
        await using var scope = services.CreateAsyncScope();

        var found = await ReadRepository(scope).FindFirstByNameAsync(Name.Create("동명이").Value, CancellationToken);

        found.Should().Be(registered[0]);
    }

    [Fact]
    [Trait("FR", "PRD-002/FR-08")]
    public async Task FindFirstByNameAsync_NfdInputWithSurroundingSpaces_FindsNfcStoredName()
    {
        // FR-08 "NFD로 보낸 이름도 조회됨": Name.Create가 Trim + NFC로 바꾸므로 저장 값(NFC)과 같게 비교된다.
        await using var services = Database.CreateServices();
        var employee = new EmployeeBuilder().WithName("홍길동").Build();
        await CommitAllAsync(services, employee);
        await using var scope = services.CreateAsyncScope();
        var nfd = "홍길동".Normalize(NormalizationForm.FormD);

        var found = await ReadRepository(scope).FindFirstByNameAsync(Name.Create($"  {nfd} ").Value, CancellationToken);

        found!.Id.Should().Be(employee.Id.Value);
    }

    [Fact]
    public async Task ReadRepository_WriteConnectionUnusable_StillReadsThroughReadConnection()
    {
        // Read Repository는 읽기 연결(EmployeeReadDbContext)만 쓴다. 쓰기 연결 비밀번호가 틀려도 목록 · 개수 · 이름 조회가 된다.
        await using (var seeding = Database.CreateServices())
        {
            await CommitAllAsync(seeding, new EmployeeBuilder().WithName("홍길동").Build());
        }

        await using var services = Database.CreateServices(new EmployeeServicesOptions
        {
            WriteConnectionString = Database.WriteConnectionStringWith(builder => builder.Password = "wrong-password"),
        });
        await using var scope = services.CreateAsyncScope();
        var repository = ReadRepository(scope);

        (await repository.ListOrderedByJoinedOnAsync(0, 10, CancellationToken)).Should().ContainSingle();
        (await repository.CountAsync(CancellationToken)).Should().Be(1);
        (await repository.FindFirstByNameAsync(Name.Create("홍길동").Value, CancellationToken)).Should().NotBeNull();
    }

    private static IEmployeeReadRepository ReadRepository(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IEmployeeReadRepository>();

    // 운영 IIdGenerator로 ID를 생성 순서대로 만들고(= 등록 순 = id 순), 저장은 섞은 순서(또는 역순)로 한다. 돌려주는 목록은 생성 순서의 기대 응답이다.
    private static async Task<List<EmployeeContactResponse>> SeedInRegistrationOrderAsync(
        ServiceProvider services, IReadOnlyList<string> joinedOns, string name = EmployeeBuilder.DefaultName, bool reverseCommit = false)
    {
        await using var scope = services.CreateAsyncScope();
        var generator = scope.ServiceProvider.GetRequiredService<IIdGenerator>();
        var employees = joinedOns.Select(joinedOn => new EmployeeBuilder().WithId(new EmployeeId(generator.NewId())).WithName(name).WithJoinedOn(joinedOn).Build()).ToList();
        var commitOrder = reverseCommit ? Enumerable.Reverse(employees) : employees.OrderBy(_ => Random.Shared.Next());
        await CommitAllAsync(services, [.. commitOrder]);

        return [.. employees.Select(employee => new EmployeeContactResponse(
            employee.Id.Value, employee.Name.Value, employee.Email.Value, employee.PhoneNumber.Value, employee.JoinedOn.Value))];
    }

    private static async Task CommitAllAsync(IServiceProvider services, params Domain.Employees.Employee[] employees)
    {
        foreach (var employee in employees)
        {
            (await EmployeeCommits.AddAndCommitAsync(services, employee, CancellationToken)).IsSuccess.Should().BeTrue();
        }
    }
}
