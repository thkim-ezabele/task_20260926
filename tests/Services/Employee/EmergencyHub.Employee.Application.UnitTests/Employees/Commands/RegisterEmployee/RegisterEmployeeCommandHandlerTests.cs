using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;
using EmergencyHub.Employee.Domain.Employees;
using EmergencyHub.Employee.Domain.Employees.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployee;

// S03-T01 등록 Handler: IIdGenerator로 ID 생성 → Register(정규화) → 정규화된 이메일로 사전 중복 검사(23001) → Add → 로그 20001.
// Handler는 저장하지 않으므로 SaveChanges · CommitAsync 호출을 검증하지 않는다(testing-strategy, ADR-0014).
// 사전 검사는 1차 방어이고 동시 요청 경합은 유니크 인덱스 → 23505 → 같은 23001 인스턴스로 막는다(T02 · T06).
public sealed class RegisterEmployeeCommandHandlerTests
{
    private static readonly Guid GeneratedId = Guid.Parse("0192a1b3-0000-7000-8000-0000000000aa");

    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly IIdGenerator _idGenerator = Substitute.For<IIdGenerator>();
    private readonly FakeLogger<RegisterEmployeeCommandHandler> _logger = new();

    public RegisterEmployeeCommandHandlerTests()
    {
        _idGenerator.NewId().Returns(GeneratedId);
    }

    // ---- 성공 ----

    [Fact]
    public async Task Handle_NewEmail_ReturnsGeneratedId()
    {
        var result = await CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new EmployeeId(GeneratedId));
        _idGenerator.Received(1).NewId();
    }

    [Fact]
    public async Task Handle_NewEmail_AddsRegisteredEmployeeWithCommandValuesAndDomainEvent()
    {
        Domain.Employees.Employee? added = null;
        _repository.Add(Arg.Do<Domain.Employees.Employee>(employee => added = employee));

        await CreateSut().Handle(
            new RegisterEmployeeCommandBuilder().WithStatus(EmployeeStatus.Inactive).Build(),
            TestContext.Current.CancellationToken);

        _repository.Received(1).Add(Arg.Any<Domain.Employees.Employee>());
        added.Should().NotBeNull();
        added!.Id.Should().Be(new EmployeeId(GeneratedId));
        added.DisplayName.Should().Be(RegisterEmployeeCommandBuilder.DefaultDisplayName);
        added.Email.Should().Be(RegisterEmployeeCommandBuilder.DefaultEmail);
        added.EmployeeStatus.Should().Be(EmployeeStatus.Inactive);
        added.DomainEvents.Should().ContainSingle().Which.Should().Be(new EmployeeRegisteredDomainEvent(new EmployeeId(GeneratedId)));
    }

    [Fact]
    public async Task Handle_NewEmail_LogsSingleEmployeeRegisteredWithEmployeeIdOnly()
    {
        await CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), TestContext.Current.CancellationToken);

        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Subject;
        record.Level.Should().Be(LogLevel.Information);
        record.Id.Id.Should().Be(20001);
        record.Id.Name.Should().Be("EmployeeRegistered");
        record.GetStructuredStateValue("EmployeeId").Should().Be(GeneratedId.ToString());
        record.StructuredState!.Select(pair => pair.Key).Should().BeEquivalentTo("EmployeeId", "{OriginalFormat}");
    }

    [Fact]
    public async Task Handle_Always_PassesCancellationTokenToRepository()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), cancellation.Token);

        await _repository.Received(1).ExistsByEmailAsync(Arg.Any<string>(), cancellation.Token);
    }

    // ---- 실패 ----

    [Fact]
    public async Task Handle_DuplicateEmail_Returns23001SameInstance()
    {
        _repository.ExistsByEmailAsync(RegisterEmployeeCommandBuilder.DefaultEmail, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_DoesNotAddOrLogRegistration()
    {
        _repository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), TestContext.Current.CancellationToken);

        _repository.DidNotReceive().Add(Arg.Any<Domain.Employees.Employee>());
        _logger.Collector.Count.Should().Be(0);
    }

    // ---- 엣지 ----

    [Theory]
    [InlineData(" Hong@Example.COM ")]
    [InlineData("HONG@EXAMPLE.COM")]
    [InlineData("\thong@example.com\n")]
    public async Task Handle_EmailDiffersOnlyByCaseOrWhitespace_ChecksDuplicateWithNormalizedEmail(string email)
    {
        _repository.ExistsByEmailAsync(RegisterEmployeeCommandBuilder.DefaultEmail, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new RegisterEmployeeCommandBuilder().WithEmail(email).Build(), TestContext.Current.CancellationToken);

        result.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
        await _repository.Received(1).ExistsByEmailAsync(RegisterEmployeeCommandBuilder.DefaultEmail, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailAndNameWithWhitespace_AddsNormalizedValues()
    {
        Domain.Employees.Employee? added = null;
        _repository.Add(Arg.Do<Domain.Employees.Employee>(employee => added = employee));
        var command = new RegisterEmployeeCommandBuilder().WithDisplayName("  홍길동 ").WithEmail(" Hong@Example.COM ").Build();

        await CreateSut().Handle(command, TestContext.Current.CancellationToken);

        added!.DisplayName.Should().Be("홍길동");
        added.Email.Should().Be("hong@example.com");
    }

    [Fact]
    public async Task Handle_MissingStatusBypassingValidation_ThrowsWithoutAdding()
    {
        // 누락은 검증 데코레이터가 21006으로 먼저 막는다. Handler까지 오면 프로그래밍 오류라 도메인 불변식이 예외로 막는다.
        var act = () => CreateSut().Handle(new RegisterEmployeeCommandBuilder().WithStatus(null).Build(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        _repository.DidNotReceive().Add(Arg.Any<Domain.Employees.Employee>());
    }

    [Fact]
    public async Task Handle_GeneratorReturnsEmptyGuid_ThrowsWithoutAdding()
    {
        _idGenerator.NewId().Returns(Guid.Empty);

        var act = () => CreateSut().Handle(new RegisterEmployeeCommandBuilder().Build(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.DidNotReceive().Add(Arg.Any<Domain.Employees.Employee>());
    }

    [Fact]
    public async Task Handle_SameCommandTwice_SecondCallReturns23001WhenFirstIsVisible()
    {
        // 같은 Command 두 번(순차): 첫 등록이 커밋되어 보이면 두 번째는 사전 검사에서 23001이다.
        _repository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false, true);
        var sut = CreateSut();
        var command = new RegisterEmployeeCommandBuilder().Build();
        (await sut.Handle(command, TestContext.Current.CancellationToken)).IsSuccess.Should().BeTrue();

        var second = await sut.Handle(command, TestContext.Current.CancellationToken);

        second.Error.Should().BeSameAs(EmployeeErrors.DuplicateEmail);
        _repository.Received(1).Add(Arg.Any<Domain.Employees.Employee>());
    }

    private RegisterEmployeeCommandHandler CreateSut() => new(_repository, _idGenerator, _logger);
}
