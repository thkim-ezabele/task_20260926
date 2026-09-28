using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace EmergencyHub.Employee.IntegrationTests.QueryPlans;

/// <summary>
/// 조회 명령(리더 실행)의 SQL 원문과 매개변수 사본을 모으는 인터셉터입니다. 명령은 바꾸지 않고 그대로 실행합니다(S05-T06 EXPLAIN 확인).
/// </summary>
/// <remarks>
/// <see cref="Fixtures.EmployeeServicesOptions.WriteInterceptors"/> · <see cref="Fixtures.EmployeeServicesOptions.ReadInterceptors"/>로 붙입니다.
/// 매개변수는 <see cref="NpgsqlParameter.Clone"/>으로 복사해 명령이 폐기된 뒤에도 다시 쓸 수 있습니다.
/// </remarks>
public sealed class CommandCaptureInterceptor : DbCommandInterceptor
{
    private readonly ConcurrentQueue<CapturedCommand> _commands = new();

    /// <summary>모은 명령(실행 순서)입니다.</summary>
    public IReadOnlyList<CapturedCommand> Commands => [.. _commands];

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Capture(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return ValueTask.FromResult(result);
    }

    private void Capture(DbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.Enqueue(new CapturedCommand(command.CommandText, [.. command.Parameters.Cast<NpgsqlParameter>().Select(parameter => parameter.Clone())]));
    }
}
