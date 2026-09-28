namespace EmergencyHub.Employee.IntegrationTests.Performance;

/// <summary>EF Core가 실행한 DB 명령 하나(매개변수 값 없이 SQL 원문 · 매개변수 개수)입니다(<see cref="CommandCountingInterceptor"/>).</summary>
/// <param name="Kind">실행 방식(<c>Reader</c> · <c>NonQuery</c> · <c>Scalar</c>).</param>
/// <param name="Text">SQL 원문.</param>
/// <param name="ParameterCount">매개변수 개수.</param>
public sealed record CountedCommand(string Kind, string Text, int ParameterCount)
{
    /// <summary>이 명령 안의 <c>INSERT INTO</c> 문 개수입니다(EF 배치 = 명령 1개에 여러 문, S06-T06 EF 배치 크기 기록).</summary>
    public int InsertStatementCount
    {
        get
        {
            var count = 0;
            for (var index = Text.IndexOf("INSERT INTO", StringComparison.Ordinal); index >= 0; index = Text.IndexOf("INSERT INTO", index + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}
