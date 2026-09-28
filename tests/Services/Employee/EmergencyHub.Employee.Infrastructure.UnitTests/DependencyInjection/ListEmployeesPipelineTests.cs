using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.ListEmployees;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.DependencyInjection;

// S07-T01(PRD-002 FR-07): 운영 DI 등록(AddEmployeeInfrastructure)의 ISender → 로깅 → 검증 → Handler 조립에서 목록 Query를 실행한다.
// Validator · Handler 단위 테스트와 HTTP 대역 테스트(ListEmployeesHttpTests)가 각각 따로 확인한 것을 잇는 부분,
// 즉 범위 밖 값이 Handler(checked skip)에 닿기 전에 1003으로 끊기는지를 본다. Read Repository만 대역이고 DB는 쓰지 않는다(전 구간은 S07-T03).
[Trait("FR", "PRD-002/FR-07")]
public sealed class ListEmployeesPipelineTests : IDisposable
{
    private readonly IEmployeeReadRepository _repository = Substitute.For<IEmployeeReadRepository>();
    private readonly ServiceProvider _provider;

    public ListEmployeesPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmployeeInfrastructure(ConnectionStringsConfiguration.Create(EmployeeDbContexts.DummyConnectionString, EmployeeDbContexts.DummyConnectionString));
        services.Replace(ServiceDescriptor.Scoped(_ => _repository));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        _repository.ListOrderedByJoinedOnAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        _repository.CountAsync(Arg.Any<CancellationToken>()).Returns(25);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _provider.Dispose();

    // ---- 성공 ----

    [Fact]
    public async Task QueryAsync_SecondPageOfTen_ReadsSkipTenThroughPipeline()
    {
        var result = await QueryAsync(ListEmployeesQuery.Create(2, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new { TotalCount = 25, Page = 2, PageSize = 10 });
        await _repository.Received(1).ListOrderedByJoinedOnAsync(10, 10, Arg.Any<CancellationToken>());
    }

    // ---- 실패: 범위 밖은 대표 1001 + 필드 1003이고 Read Repository를 부르지 않는다 ----

    [Fact]
    public async Task QueryAsync_PageZeroAndPageSize101_ReturnsFieldCode1003WithoutQuerying()
    {
        var result = await QueryAsync(ListEmployeesQuery.Create(0, 101));

        AssertInvalidPagingOnBothFields(result.IsFailure, result.Error);
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    // ---- 엣지: skip이 int를 넘는 값도 Handler의 checked(OverflowException, 500)까지 가지 않는다 ----

    [Fact]
    public async Task QueryAsync_IntMaxPageAndIntMinPageSize_ReturnsFieldCode1003InsteadOfOverflow()
    {
        var result = await QueryAsync(ListEmployeesQuery.Create(int.MaxValue, int.MinValue));

        AssertInvalidPagingOnBothFields(result.IsFailure, result.Error);
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task QueryAsync_UpperBoundPageAndPageSize_PassesValidationWithLargestSkip()
    {
        var result = await QueryAsync(ListEmployeesQuery.Create(ListEmployeesQuery.MaxPage, ListEmployeesQuery.MaxPageSize));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty("마지막 쪽을 넘으면 빈 목록이다");
        result.Value.TotalCount.Should().Be(25);
        await _repository.Received(1).ListOrderedByJoinedOnAsync(9_999_900, 100, Arg.Any<CancellationToken>());
    }

    private static void AssertInvalidPagingOnBothFields(bool isFailure, Error error)
    {
        isFailure.Should().BeTrue();
        var validation = error.Should().BeOfType<ValidationError>().Subject;
        validation.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        validation.Errors.Select(field => (field.PropertyName, field.Code))
            .Should().Equal((nameof(ListEmployeesQuery.Page), 1003), (nameof(ListEmployeesQuery.PageSize), 1003));
    }

    private async Task<Result<ListEmployeesResponse>> QueryAsync(ListEmployeesQuery query)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().QueryAsync(query, CancellationToken);
    }
}
