using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>
/// 실행 전략의 재시도 설정(<c>ExecutionStrategy</c>의 protected 속성 MaxRetryCount · MaxRetryDelay)을 리플렉션으로 읽는다.
/// 재시도 인자가 공통 옵션 구성까지 전달되는지 DB 없이 확인하려는 용도다(BL-073).
/// </remarks>
public static class ExecutionStrategySettings
{
    public static int MaxRetryCount(IExecutionStrategy strategy) => Read<int>(strategy, "MaxRetryCount");

    public static TimeSpan MaxRetryDelay(IExecutionStrategy strategy) => Read<TimeSpan>(strategy, "MaxRetryDelay");

    private static T Read<T>(IExecutionStrategy strategy, string propertyName) =>
        (T)typeof(ExecutionStrategy)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(strategy)!;
}
