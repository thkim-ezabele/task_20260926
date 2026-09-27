namespace EmergencyHub.BuildingBlocks.Application.Pipeline;

/// <summary>
/// Handler 데코레이터 파이프라인의 open generic 형식 목록입니다(ADR-0015). 순서는 <b>안쪽 → 바깥</b>입니다.
/// </summary>
/// <remarks>
/// <para>
/// 데코레이터는 <c>internal</c>이므로 Infrastructure의 <c>AddConventionalServices</c>가 이 목록을 받아
/// 앞에서부터 차례로 Scrutor <c>TryDecorate</c>를 부릅니다(ADR-0017). 먼저 감싼 것이 안쪽이 되므로
/// 해석 결과는 Command가 로깅 → 검증 → 트랜잭션 → Handler, Query가 로깅 → 검증 → Handler입니다.
/// </para>
/// <para>
/// 파이프라인 순서의 원본은 이 목록 하나입니다. 데코레이터를 추가 · 재배치할 때는 여기만 고칩니다.
/// </para>
/// </remarks>
public static class PipelineDecorators
{
    /// <summary>
    /// <c>ICommandHandler&lt;,&gt;</c> 데코레이터(안쪽 → 바깥): 트랜잭션, 검증, 로깅.
    /// </summary>
    public static IReadOnlyList<Type> CommandHandlerDecorators { get; } = Array.AsReadOnly(
    [
        typeof(TransactionCommandHandlerDecorator<,>),
        typeof(ValidationCommandHandlerDecorator<,>),
        typeof(LoggingCommandHandlerDecorator<,>),
    ]);

    /// <summary>
    /// <c>IQueryHandler&lt;,&gt;</c> 데코레이터(안쪽 → 바깥): 검증, 로깅. Query에는 트랜잭션이 없습니다(ADR-0007).
    /// </summary>
    public static IReadOnlyList<Type> QueryHandlerDecorators { get; } = Array.AsReadOnly(
    [
        typeof(ValidationQueryHandlerDecorator<,>),
        typeof(LoggingQueryHandlerDecorator<,>),
    ]);
}
