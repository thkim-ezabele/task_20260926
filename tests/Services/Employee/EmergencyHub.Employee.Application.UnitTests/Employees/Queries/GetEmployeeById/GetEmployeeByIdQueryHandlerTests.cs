using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeById;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Queries.GetEmployeeById;

// S03-T01 단건 조회: Read Repository(읽기 DbContext 프로젝션)로만 조회하고, 없으면 22001.
// 프로젝션 자체(정규화된 이메일, 감사 시각)는 통합 테스트가 검증한다(testing-strategy "단위 테스트").
public sealed class GetEmployeeByIdQueryHandlerTests
{
    private static readonly Guid ExistingId = Guid.Parse("0192a1b3-0000-7000-8000-000000000001");

    private readonly IEmployeeReadRepository _readRepository = Substitute.For<IEmployeeReadRepository>();

    // ---- 성공 ----

    [Fact]
    public async Task Handle_ExistingId_ReturnsProjectedResponse()
    {
        var response = new EmployeeResponse(
            ExistingId,
            "홍길동",
            "hong@example.com",
            EmployeeStatus.Active,
            new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
        _readRepository.GetByIdAsync(ExistingId, Arg.Any<CancellationToken>()).Returns(response);

        var result = await CreateSut().Handle(new GetEmployeeByIdQuery(ExistingId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Handle_Always_PassesIdAndCancellationTokenToReadRepository()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateSut().Handle(new GetEmployeeByIdQuery(ExistingId), cancellation.Token);

        await _readRepository.Received(1).GetByIdAsync(ExistingId, cancellation.Token);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Handle_MissingId_Returns22001SameInstance()
    {
        _readRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((EmployeeResponse?)null);

        var result = await CreateSut().Handle(new GetEmployeeByIdQuery(ExistingId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.NotFound);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task Handle_EmptyGuid_QueriesAndReturns22001()
    {
        // 경로 제약이 없으므로(계획 리뷰 결정) 빈 Guid도 조회해 22001로 응답한다. 형식 오류(1001)는 API 바인딩 몫이다.
        var result = await CreateSut().Handle(new GetEmployeeByIdQuery(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().BeSameAs(EmployeeErrors.NotFound);
        await _readRepository.Received(1).GetByIdAsync(Guid.Empty, Arg.Any<CancellationToken>());
    }

    private GetEmployeeByIdQueryHandler CreateSut() => new(_readRepository);
}
