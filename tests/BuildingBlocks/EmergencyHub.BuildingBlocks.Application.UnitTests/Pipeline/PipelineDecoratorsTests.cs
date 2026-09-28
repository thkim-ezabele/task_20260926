using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Pipeline;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Pipeline;

// 데코레이터 open generic 목록(안쪽 → 바깥). AddConventionalServices(Infrastructure)가 이 순서대로 TryDecorate한다(ADR-0015, ADR-0017).
public sealed class PipelineDecoratorsTests
{
    // ---- 성공 ----

    [Fact]
    public void CommandHandlerDecorators_InnerToOuter_IsTransactionValidationLogging()
    {
        PipelineDecorators.CommandHandlerDecorators.Should().Equal(
            typeof(TransactionCommandHandlerDecorator<,>),
            typeof(ValidationCommandHandlerDecorator<,>),
            typeof(LoggingCommandHandlerDecorator<,>));
    }

    [Fact]
    public void QueryHandlerDecorators_InnerToOuter_IsValidationLogging()
    {
        PipelineDecorators.QueryHandlerDecorators.Should().Equal(
            typeof(ValidationQueryHandlerDecorator<,>),
            typeof(LoggingQueryHandlerDecorator<,>));
    }

    [Fact]
    public void CommandHandlerDecorators_EveryEntry_IsOpenGenericImplementingCommandHandler()
    {
        PipelineDecorators.CommandHandlerDecorators.Should().OnlyContain(type =>
            type.IsGenericTypeDefinition
            && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)));
    }

    [Fact]
    public void QueryHandlerDecorators_EveryEntry_IsOpenGenericImplementingQueryHandler()
    {
        PipelineDecorators.QueryHandlerDecorators.Should().OnlyContain(type =>
            type.IsGenericTypeDefinition
            && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));
    }

    // ---- 실패: 목록을 밖에서 바꿀 수 없다 ----

    [Fact]
    public void CommandHandlerDecorators_CastToMutableList_ThrowsNotSupportedException()
    {
        var act = () => ((IList<Type>)PipelineDecorators.CommandHandlerDecorators).Add(typeof(object));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void QueryHandlerDecorators_CastToMutableList_ThrowsNotSupportedException()
    {
        var act = () => ((IList<Type>)PipelineDecorators.QueryHandlerDecorators).Clear();

        act.Should().Throw<NotSupportedException>();
    }

    // ---- 엣지 ----

    [Fact]
    public void QueryHandlerDecorators_Always_DoesNotContainTransactionDecorator()
    {
        // Query는 트랜잭션 없음(ADR-0007, ADR-0015).
        PipelineDecorators.QueryHandlerDecorators.Should().NotContain(typeof(TransactionCommandHandlerDecorator<,>));
    }

    [Fact]
    public void Decorators_Outermost_IsLoggingDecorator()
    {
        PipelineDecorators.CommandHandlerDecorators[^1].Should().Be(typeof(LoggingCommandHandlerDecorator<,>));
        PipelineDecorators.QueryHandlerDecorators[^1].Should().Be(typeof(LoggingQueryHandlerDecorator<,>));
    }
}
