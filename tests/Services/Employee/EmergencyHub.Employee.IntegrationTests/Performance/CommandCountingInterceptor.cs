using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.Employee.IntegrationTests.Performance;

/// <summary>
/// DB 명령 실행(리더 · 비쿼리 · 스칼라, 동기 · 비동기)을 세는 인터셉터입니다(S06-T06 EF 배치 크기 = 명령 분할 수 측정). 명령은 바꾸지 않습니다.
/// </summary>
/// <remarks>
/// 매개변수 값은 복사하지 않습니다(개수만, 1,000행 × 9개 복사 비용과 개인정보 보관을 피함). 실행 뒤가 아니라 실행 직전에 셉니다(실패한 명령도 포함).
/// <see cref="Fixtures.EmployeeApiFactoryOptions.WriteInterceptors"/>로 붙이고, 측정 구간 앞에서 <see cref="Clear"/>합니다.
/// </remarks>
public sealed class CommandCountingInterceptor : DbCommandInterceptor
{
    private readonly ConcurrentQueue<CountedCommand> _commands = new();

    /// <summary>센 명령(실행 순서)입니다.</summary>
    public IReadOnlyList<CountedCommand> Commands => [.. _commands];

    /// <summary><c>INSERT INTO</c>가 들어 있는 명령(= EF 저장 배치)입니다.</summary>
    public IReadOnlyList<CountedCommand> InsertCommands => [.. _commands.Where(command => command.InsertStatementCount > 0)];

    /// <summary>센 명령을 비웁니다.</summary>
    public void Clear() => _commands.Clear();

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Count("Reader", command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count("Reader", command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Count("NonQuery", command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Count("NonQuery", command);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Count("Scalar", command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Count("Scalar", command);
        return ValueTask.FromResult(result);
    }

    private void Count(string kind, DbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.Enqueue(new CountedCommand(kind, command.CommandText, command.Parameters.Count));
    }
}
