using System.Reflection;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Application.Persistence;
using EmergencyHub.BuildingBlocks.Application.Services;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Contracts;

// 계약(인터페이스)의 모양이 ADR-0014 · 0015 · 0024와 dba 진입 점검 조건을 지키는지 확인한다.
public sealed class ContractShapeTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    public void ICommand_WithoutResponse_IsCommandOfUnit()
    {
        typeof(ICommand<Unit>).IsAssignableFrom(typeof(ICommand)).Should().BeTrue();
        typeof(ICommand).GetMembers().Should().BeEmpty("ICommand는 ICommand<Unit>의 별칭 역할만 한다");
        typeof(ICommand<>).GetMembers().Should().BeEmpty();
        typeof(IQuery<>).GetMembers().Should().BeEmpty();
    }

    [Fact]
    public void ICommandHandlerOfCommand_IsCommandHandlerOfCommandAndUnit()
    {
        typeof(ICommandHandler<SampleCommand, Unit>).IsAssignableFrom(typeof(ICommandHandler<SampleCommand>)).Should().BeTrue();
        typeof(ICommandHandler<>).GetMembers(DeclaredPublicInstance).Should().BeEmpty("편의 인터페이스라 멤버를 새로 선언하지 않는다");
    }

    [Theory]
    [InlineData(typeof(ICommandHandler<CreateSampleCommand, Guid>), typeof(CreateSampleCommand), typeof(Result<Guid>))]
    [InlineData(typeof(IQueryHandler<GetSampleQuery, SampleResponse>), typeof(GetSampleQuery), typeof(Result<SampleResponse>))]
    public void HandlerInterfaces_DeclareOnlyHandleWithRequestAndCancellationToken(Type handlerType, Type requestType, Type resultType)
    {
        var methods = handlerType.GetMethods(DeclaredPublicInstance);

        methods.Should().ContainSingle();
        var handle = methods[0];
        handle.Name.Should().Be("Handle");
        handle.ReturnType.Should().Be(typeof(Task<>).MakeGenericType(resultType));
        handle.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(requestType, typeof(CancellationToken));
    }

    [Fact]
    public void ISender_DeclaresOnlySendAsyncAndQueryAsync()
    {
        var methods = typeof(ISender).GetMethods(DeclaredPublicInstance);

        methods.Select(method => method.Name).Should().BeEquivalentTo(["SendAsync", "QueryAsync"]);
        methods.Should().AllSatisfy(method =>
        {
            method.IsGenericMethodDefinition.Should().BeTrue();
            method.GetParameters().Last().ParameterType.Should().Be(typeof(CancellationToken));
            method.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>));
        });
    }

    [Fact]
    public void IUnitOfWork_ExposesOnlyCommitAsyncReturningTaskOfResult()
    {
        var members = typeof(IUnitOfWork).GetMembers(DeclaredPublicInstance);

        // Handler는 저장하지 않는다(ADR-0014). SaveChanges · BeginTransaction을 공개하지 않는다(T01 dba 진입 점검).
        members.Should().ContainSingle();
        var commit = typeof(IUnitOfWork).GetMethod("CommitAsync");
        commit.Should().NotBeNull();
        commit!.ReturnType.Should().Be(typeof(Task<Result>));
        commit.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(typeof(CancellationToken));
        typeof(IUnitOfWork).GetInterfaces().Should().BeEmpty("IDisposable 등 다른 계약을 끌어오지 않는다");
    }

    [Theory]
    [InlineData(typeof(IReadRepository))]
    [InlineData(typeof(IService))]
    public void MarkerInterfaces_HaveNoMembers(Type markerType)
    {
        markerType.IsInterface.Should().BeTrue();
        markerType.GetMembers().Should().BeEmpty("{0}는 DI 자동 등록용 마커다(ADR-0010)", markerType.Name);
    }

    [Fact]
    public void IIdGenerator_DeclaresOnlyNewIdReturningGuid()
    {
        var methods = typeof(IIdGenerator).GetMethods(DeclaredPublicInstance);

        methods.Should().ContainSingle();
        methods[0].Name.Should().Be("NewId");
        methods[0].ReturnType.Should().Be(typeof(Guid));
        methods[0].GetParameters().Should().BeEmpty();
    }

    [Fact]
    public void IExceptionClassifier_TakesOnlySystemExceptionAndReturnsNullableError()
    {
        var methods = typeof(IExceptionClassifier).GetMethods(DeclaredPublicInstance);

        methods.Should().ContainSingle();
        var classify = methods[0];
        classify.Name.Should().Be("Classify");
        classify.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal([typeof(Exception)], "EF · Npgsql 예외 형식이 Application 계약에 새지 않는다(ADR-0024)");
        classify.ReturnType.Should().Be(typeof(Error));
        new NullabilityInfoContext().Create(classify.ReturnParameter).ReadState
            .Should().Be(NullabilityState.Nullable, "분류하지 못하면 null을 돌려준다");
    }
}
