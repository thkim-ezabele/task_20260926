using System.Data.Common;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// 변환되지 않은 DB 예외(23514 · 25006) 대역입니다. BuildingBlocks.Api는 Npgsql을 참조하지 않으므로(ADR-0024)
/// <c>PostgresException</c>과 같은 기반(<see cref="DbException"/>)과 메시지 형식만 흉내 냅니다.
/// </summary>
public sealed class SampleDbException : DbException
{
    public SampleDbException()
        : this(string.Empty, string.Empty)
    {
    }

    public SampleDbException(string message)
        : this(message, string.Empty)
    {
    }

    public SampleDbException(string message, Exception innerException)
        : base(message, innerException)
    {
        SqlState = string.Empty;
    }

    public SampleDbException(string message, string sqlState)
        : base(message)
    {
        SqlState = sqlState;
    }

    public override string SqlState { get; }
}
