using EmergencyHub.Employee.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace EmergencyHub.Employee.MigrationService.UnitTests;

// S03-T03 · database.md "MigrationService 동작 사양": 성공 0, 예외 · 취소는 0이 아닌 코드를 명시하고 StopApplication.
// 로그는 성공 Information 1건(20901) · 실패 Error 1건(20902), 속성에 연결 정보 없음.
// 적용 동작(실행 전략 안 MigrateAsync)은 대역으로 바꾸고, 종료 코드는 반환값과 대역 setter로 확인한다(Environment.ExitCode는 프로세스 전역).
[Trait("FR", "PRD-001/FR-09")]
public sealed class MigrationWorkerTests
{
    private const string SecretConnection = "Host=db.internal;Username=employee_app;Password=do-not-leak";

    private readonly FakeLogCollector _logs = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly IHostApplicationLifetime _lifetime = Substitute.For<IHostApplicationLifetime>();
    private readonly List<int> _exitCodes = [];

    [Fact]
    public async Task RunAsync_MigrationSucceeds_ReturnsZeroAndLogsInformation()
    {
        var worker = CreateWorker((_, _) =>
        {
            _timeProvider.Advance(TimeSpan.FromMilliseconds(250));
            return Task.CompletedTask;
        });

        var exitCode = await worker.RunAsync(TestContext.Current.CancellationToken);

        exitCode.Should().Be(MigrationExitCode.Succeeded);
        ((int)exitCode).Should().Be(0);
        var record = _logs.GetSnapshot().Should().ContainSingle().Which;
        record.Id.Id.Should().Be(20901);
        record.Level.Should().Be(LogLevel.Information);
        record.GetStructuredStateValue("DbContextType").Should().Be(nameof(EmployeeDbContext));
        record.GetStructuredStateValue("ElapsedMilliseconds").Should().Be("250");
    }

    [Fact]
    public async Task RunAsync_MigrationThrows_ReturnsFailedAndLogsSingleErrorWithException()
    {
        var failure = new InvalidOperationException(SecretConnection);
        var worker = CreateWorker((_, _) => throw failure);

        var exitCode = await worker.RunAsync(TestContext.Current.CancellationToken);

        exitCode.Should().Be(MigrationExitCode.Failed);
        ((int)exitCode).Should().NotBe(0);
        var record = _logs.GetSnapshot().Should().ContainSingle().Which;
        record.Id.Id.Should().Be(20902);
        record.Level.Should().Be(LogLevel.Error);
        record.Exception.Should().BeSameAs(failure);
        record.GetStructuredStateValue("DbContextType").Should().Be(nameof(EmployeeDbContext));
        record.GetStructuredStateValue("ExceptionType").Should().Be(typeof(InvalidOperationException).FullName);
        record.GetStructuredStateValue("SqlState").Should().BeNull();
        record.GetStructuredStateValue("ExitCode").Should().Be("1");
    }

    [Fact]
    public async Task RunAsync_PostgresExceptionWrappedByRetryStrategy_LogsInnerSqlState()
    {
        // 실행 전략이 재시도 한도를 넘기면 원인 예외를 안쪽에 감싼다. SqlState는 사슬에서 찾는다.
        var postgres = new PostgresException("password authentication failed", "FATAL", "FATAL", "28P01");
        var worker = CreateWorker((_, _) => throw new InvalidOperationException("retry limit exceeded", postgres));

        var exitCode = await worker.RunAsync(TestContext.Current.CancellationToken);

        exitCode.Should().Be(MigrationExitCode.Failed);
        var record = _logs.GetSnapshot().Should().ContainSingle().Which;
        record.GetStructuredStateValue("SqlState").Should().Be("28P01");
        record.GetStructuredStateValue("ExceptionType").Should().Be(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task RunAsync_Canceled_ReturnsCanceledNonZeroAndLogsError()
    {
        // 취소는 성공이 아니다(AppHost WaitForCompletion이 Api를 띄우지 않아야 함).
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var worker = CreateWorker((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });

        var exitCode = await worker.RunAsync(cancellation.Token);

        exitCode.Should().Be(MigrationExitCode.Canceled);
        ((int)exitCode).Should().NotBe(0);
        var record = _logs.GetSnapshot().Should().ContainSingle().Which;
        record.Id.Id.Should().Be(20902);
        record.Level.Should().Be(LogLevel.Error);
        record.Exception.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_Failure_LogPropertiesCarryNoConnectionInformation()
    {
        var worker = CreateWorker((_, _) => throw new InvalidOperationException(SecretConnection));

        await worker.RunAsync(TestContext.Current.CancellationToken);

        var record = _logs.GetSnapshot().Should().ContainSingle().Which;
        record.StructuredState!.Select(pair => pair.Key).Should().BeEquivalentTo(
            "DbContextType", "ExceptionType", "SqlState", "ElapsedMilliseconds", "ExitCode", "{OriginalFormat}");
        record.Message.Should().NotContain("do-not-leak").And.NotContain("db.internal").And.NotContain("employee_app");
    }

    [Fact]
    public async Task RunAsync_Called_PassesScopedProviderAndToken()
    {
        IServiceProvider? received = null;
        var token = TestContext.Current.CancellationToken;
        CancellationToken receivedToken = default;
        var worker = CreateWorker((services, ct) =>
        {
            received = services;
            receivedToken = ct;
            return Task.CompletedTask;
        });

        await worker.RunAsync(token);

        received.Should().NotBeNull();
        receivedToken.Should().Be(token);
    }

    [Fact]
    public async Task ExecuteAsync_MigrationSucceeds_SetsExitCodeZeroAndStopsApplication()
    {
        var worker = CreateWorker((_, _) => Task.CompletedTask);

        await RunToCompletionAsync(worker);

        _exitCodes.Should().Equal(0);
        _lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task ExecuteAsync_MigrationThrows_SetsNonZeroExitCodeAndStopsApplication()
    {
        var worker = CreateWorker((_, _) => throw new InvalidOperationException("boom"));

        await RunToCompletionAsync(worker);

        _exitCodes.Should().Equal((int)MigrationExitCode.Failed);
        _lifetime.Received(1).StopApplication();
    }

    [Fact]
    public void ExitCodes_AreExplicitIntegers()
    {
        ((int)MigrationExitCode.Succeeded).Should().Be(0);
        ((int)MigrationExitCode.Failed).Should().Be(1);
        ((int)MigrationExitCode.Canceled).Should().Be(2);
        Enum.GetUnderlyingType(typeof(MigrationExitCode)).Should().Be<short>();
    }

    private static async Task RunToCompletionAsync(MigrationWorker worker)
    {
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!;
    }

    private MigrationWorker CreateWorker(Func<IServiceProvider, CancellationToken, Task> migrate)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new FakeLogger<MigrationWorker>(_logs);

        return new MigrationWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            _lifetime,
            _timeProvider,
            logger,
            migrate,
            _exitCodes.Add);
    }
}
