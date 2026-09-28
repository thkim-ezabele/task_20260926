using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Application.Employees;
using EmergencyHub.Employee.Application.Employees.Queries.GetEmployeeByName;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.DependencyInjection;

// S07-T02(PRD-002 FR-08): 운영 DI 등록(AddEmployeeInfrastructure)의 ISender → 로깅 → 검증 → Handler 조립에서 이름 조회 Query를 실행한다.
// 이름 규칙 실패가 Read Repository에 닿기 전에 대표 1001 + 필드 코드(21007 · 21008 · 21009)로 끊기는지, 통과한 이름이 Trim + NFC로 조회되는지 본다(DB는 S07-T03).
[Trait("FR", "PRD-002/FR-08")]
public sealed class GetEmployeeByNamePipelineTests : IDisposable
{
    private readonly IEmployeeReadRepository _repository = Substitute.For<IEmployeeReadRepository>();
    private readonly ServiceProvider _provider;

    public GetEmployeeByNamePipelineTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmployeeInfrastructure(ConnectionStringsConfiguration.Create(EmployeeDbContexts.DummyConnectionString, EmployeeDbContexts.DummyConnectionString));
        services.Replace(ServiceDescriptor.Scoped(_ => _repository));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose() => _provider.Dispose();

    // ---- 성공 ----

    [Fact]
    public async Task QueryAsync_PaddedNfdName_QueriesTrimmedNfcNameThroughPipeline()
    {
        var contact = new EmployeeContactResponse(Guid.NewGuid(), "홍길동", "hong@example.com", "010-1234-5678", new DateOnly(2020, 1, 2));
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns(contact);

        var result = await QueryAsync(new GetEmployeeByNameQuery(" " + "홍길동".Normalize(System.Text.NormalizationForm.FormD) + " "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(contact.Id);
        await _repository.Received(1).FindFirstByNameAsync(Arg.Is<Name>(name => name.Value == "홍길동"), Arg.Any<CancellationToken>());
    }

    // ---- 실패 ----

    [Theory]
    [InlineData(null, 21007)]
    [InlineData("   ", 21007)]
    [InlineData("홍\u0001길동", 21009)]
    public async Task QueryAsync_InvalidName_ReturnsFieldCodeWithoutQuerying(string? name, int code)
    {
        var result = await QueryAsync(new GetEmployeeByNameQuery(name));

        result.IsFailure.Should().BeTrue();
        var validation = result.Error.Should().BeOfType<ValidationError>().Subject;
        validation.Code.Should().Be(CommonErrors.ValidationFailed.Code);
        validation.Errors.Select(field => (field.PropertyName, field.Code)).Should().Equal((nameof(GetEmployeeByNameQuery.Name), code));
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task QueryAsync_NotFound_Returns22001()
    {
        _repository.FindFirstByNameAsync(Arg.Any<Name>(), Arg.Any<CancellationToken>()).Returns((EmployeeContactResponse?)null);

        var result = await QueryAsync(new GetEmployeeByNameQuery("없는사람"));

        result.Error.Should().BeSameAs(EmployeeErrors.NotFound);
    }

    // ---- 엣지 ----

    [Fact]
    public async Task QueryAsync_TooLongName_Returns21008WithoutQuerying()
    {
        var result = await QueryAsync(new GetEmployeeByNameQuery(new string('가', Name.MaxLength + 1)));

        result.Error.Should().BeOfType<ValidationError>().Which.Errors.Should().ContainSingle().Which.Code.Should().Be(21008);
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    private async Task<Result<EmployeeResponse>> QueryAsync(GetEmployeeByNameQuery query)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().QueryAsync(query, CancellationToken);
    }
}
