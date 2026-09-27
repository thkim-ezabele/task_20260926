using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.Employee.IntegrationTests.FaultInjection;

/// <summary>
/// 명령 실행 직전에 정한 횟수만큼 예외를 던지는 인터셉터입니다(testing-strategy.md "장애 주입": 트리거 대안, 서버에 보내기 전에 끊음).
/// </summary>
/// <remarks>
/// <see cref="Fixtures.EmployeeServicesOptions.WriteInterceptors"/>로 쓰기 DbContext에만 붙입니다. 조건(<c>shouldFail</c>)에 맞는 명령만 세고,
/// 남은 실패 횟수가 있으면 <c>failure()</c>가 만든 예외를 던집니다. 스레드 안전합니다(병렬 스코프 가능).
/// </remarks>
/// <param name="failure">던질 예외를 만드는 함수(예: <see cref="InjectedFailures.SerializationFailure"/>).</param>
/// <param name="failures">실패 횟수(0 이상, <see cref="int.MaxValue"/>면 항상).</param>
/// <param name="shouldFail">대상 명령 조건. <see langword="null"/>이면 모든 명령.</param>
public sealed class CommandFaultInterceptor(Func<Exception> failure, int failures, Func<DbCommand, bool>? shouldFail = null) : DbCommandInterceptor
{
    private int _remaining = failures >= 0 ? failures : throw new ArgumentOutOfRangeException(nameof(failures));
    private int _attempts;

    /// <summary>조건에 맞은 명령 실행 시도 수(실패 · 통과 합계)입니다.</summary>
    public int Attempts => Volatile.Read(ref _attempts);

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Intercept(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Intercept(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Intercept(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Intercept(command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Intercept(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Intercept(command);
        return ValueTask.FromResult(result);
    }

    private void Intercept(DbCommand command)
    {
        if (shouldFail is not null && !shouldFail(command))
        {
            return;
        }

        Interlocked.Increment(ref _attempts);

        if (failures == int.MaxValue || Interlocked.Decrement(ref _remaining) >= 0)
        {
            throw failure();
        }
    }
}
