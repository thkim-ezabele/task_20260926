using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EmergencyHub.Employee.Infrastructure.UnitTests.TestDoubles;

/// <remarks>
/// DB 없이 Repository 쿼리를 실행하는 대역이다. 연결 열기를 건너뛰고, 쿼리 명령을 가로채 SQL · 매개변수 값을 기록한 뒤
/// 지정한 결과 표(DataTable)를 리더로 돌려준다. SQL이 실제 DB에서 도는지는 S03-T06 통합 테스트가 확인한다.
/// </remarks>
internal sealed class FakeQueryDatabase(DataTable result) : DbCommandInterceptor, IDbConnectionInterceptor
{
    private readonly List<string> _commandTexts = [];
    private readonly List<object?> _parameterValues = [];

    public IReadOnlyList<string> CommandTexts => _commandTexts;

    public IReadOnlyList<object?> ParameterValues => _parameterValues;

    public InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
        InterceptionResult.Suppress();

    public ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(InterceptionResult.Suppress());

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result) =>
        Respond(command);

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Respond(command));

    private InterceptionResult<DbDataReader> Respond(DbCommand command)
    {
        _commandTexts.Add(command.CommandText);
        _parameterValues.AddRange(command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value));
        return InterceptionResult<DbDataReader>.SuppressWithResult(result.CreateDataReader());
    }
}
