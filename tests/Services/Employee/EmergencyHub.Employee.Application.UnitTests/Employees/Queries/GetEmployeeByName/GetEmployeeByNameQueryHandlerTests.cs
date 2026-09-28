using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.GetEmployeeByName;

// S07-T02(PRD-002 FR-08): Handler는 입력을 Name.Create(Trim + NFC)로 바꿔 Read Repository의 정확 일치(대소문자 구분) 조회에 넘긴다.
// 동명이인 순서(joined_on → id)와 첫 1명 선택은 Repository 쿼리 몫이고 Handler는 받은 1명을 응답으로 옮긴다. 없으면 404 · 22001.
[Trait("FR", "PRD-002/FR-08")]
[Trait("FR", "PRD-002/FR-10")]
public sealed class GetEmployeeByNameQueryHandlerTests
{
    private static readonly EmployeeContactResponse Hong =
        new(Guid.NewGuid(), "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2));

    private readonly IEmployeeReadRepository _repository = Substitute.For<IEmployeeReadRepository>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // ---- 성공 ----

    [Fact]
    public async Task Handle_Found_ReturnsContactAsEmployeeResponse()
    {
        _repository.FindFirstByNameAsync(Name.Create("홍길동").Value, Arg.Any<CancellationToken>()).Returns(Hong);

        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery("홍길동"), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new EmployeeResponse(Hong.Id, "홍길동", "Hong@Example.com", "010-1234-5678", new DateOnly(2020, 1, 2)));
    }

    [Fact]
    public async Task Handle_SurroundingSpaces_QueriesTrimmedName()
    {
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns(Hong);

        await CreateHandler().Handle(new GetEmployeeByNameQuery("  홍길동\t"), CancellationToken);

        await _repository.Received(1).FindFirstByNameAsync(Arg.Is<Name>(name => name.Value == "홍길동"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NfdInput_QueriesNfcName()
    {
        var nfd = "홍길동".Normalize(NormalizationForm.FormD);
        nfd.Should().NotBe("홍길동");
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns(Hong);

        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery(nfd), CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).FindFirstByNameAsync(Arg.Is<Name>(name => name.Value == "홍길동"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        using var cancellation = new CancellationTokenSource();
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns(Hong);

        await CreateHandler().Handle(new GetEmployeeByNameQuery("홍길동"), cancellation.Token);

        await _repository.Received(1).FindFirstByNameAsync(Arg.Any<Name>(), cancellation.Token);
        await _repository.DidNotReceiveWithAnyArgs().ListOrderedByJoinedOnAsync(default, default, CancellationToken);
        await _repository.DidNotReceiveWithAnyArgs().CountAsync(CancellationToken);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Handle_NotFound_Returns22001NotFound()
    {
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns((EmployeeContactResponse?)null);

        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery("없는사람"), CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NotFound);
        result.Error.Code.Should().Be(22001);
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().NotContain("없는사람", "NFR-04: detail에 이름 값이 없다");
    }

    [Fact]
    public async Task Handle_RepositoryThrows_PropagatesException()
    {
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>())
            .Returns<Task<EmployeeContactResponse?>>(_ => throw new InvalidOperationException("db"));

        var act = () => CreateHandler().Handle(new GetEmployeeByNameQuery("홍길동"), CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(" ", 21007)]
    [InlineData("홍\u0001길동", 21009)]
    public async Task Handle_InvalidNameWithoutValidator_ReturnsNameErrorWithoutQuerying(string name, int code)
    {
        // Validator를 거치지 않은 값이어도 Name.Create 결과를 그대로 돌려주고 조회하지 않는다(판정 원본은 Name).
        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery(name), CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(code);
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_DifferentCase_QueriesAsIsWithoutCaseFolding()
    {
        // 대소문자 구분: "hong"을 "Hong"으로 바꾸거나 소문자로 접지 않는다. 저장 값 "Hong"과는 DB에서 일치하지 않아 404다.
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns((EmployeeContactResponse?)null);

        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery("hong"), CancellationToken);

        await _repository.Received(1).FindFirstByNameAsync(Arg.Is<Name>(name => name.Value == "hong"), Arg.Any<CancellationToken>());
        result.Error.Should().BeSameAs(EmployeeErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Homonyms_ReturnsTheOneRepositoryPicked()
    {
        // 동명이인 중 입사일 → ID 순 첫 1명은 Repository(ORDER BY joined_on, id LIMIT 1)가 고른다. Handler는 다시 고르지 않는다.
        var earliest = Hong with { Id = Guid.NewGuid(), JoinedOn = new DateOnly(2010, 5, 1) };
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns(earliest);

        var result = await CreateHandler().Handle(new GetEmployeeByNameQuery("홍길동"), CancellationToken);

        result.Value.Id.Should().Be(earliest.Id);
        result.Value.Joined.Should().Be(new DateOnly(2010, 5, 1));
        await _repository.Received(1).FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InternalSpaces_AreKept()
    {
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns((EmployeeContactResponse?)null);

        await CreateHandler().Handle(new GetEmployeeByNameQuery(" Hong  Gildong "), CancellationToken);

        await _repository.Received(1).FindFirstByNameAsync(Arg.Is<Name>(name => name.Value == "Hong  Gildong"), Arg.Any<CancellationToken>());
    }

    private GetEmployeeByNameQueryHandler CreateHandler() => new(_repository);
}
