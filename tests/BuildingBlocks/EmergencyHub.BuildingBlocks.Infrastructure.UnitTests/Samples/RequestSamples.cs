using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Validation;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using FluentValidation;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// Validator · Handler가 불렸는지와 순서를 기록하는 탐침입니다. 마커를 상속하지 않으므로 테스트가 대역을 직접 등록합니다.
/// </summary>
public interface IPipelineProbe
{
    /// <summary>Validator 규칙이 실행될 때 부릅니다.</summary>
    /// <param name="request">검증 중인 요청.</param>
    void Validating(object request);

    /// <summary>Handler 본문이 실행될 때 부릅니다.</summary>
    /// <param name="request">처리 중인 요청.</param>
    void Handling(object request);
}

/// <summary>테스트용 오류 정의(서비스 자리 2 가정).</summary>
public static class SampleErrors
{
    /// <summary>값이 음수(검증 실패).</summary>
    public static readonly Error ValueNegative = Error.Validation(21001, "값은 0 이상이어야 합니다.");

    /// <summary>Handler가 돌려주는 규칙 실패(충돌).</summary>
    public static readonly Error ValueConflict = Error.Conflict(23001, "이미 처리된 값입니다.");
}

/// <summary>ID를 돌려주는 Command 예시. <see cref="Value"/>가 음수면 검증 실패, 0이면 Handler 실패.</summary>
/// <param name="Value">값.</param>
public sealed record CreateSampleCommand(int Value) : ICommand<Guid>;

/// <summary><see cref="CreateSampleCommand"/> Handler.</summary>
public sealed class CreateSampleCommandHandler(IPipelineProbe probe) : ICommandHandler<CreateSampleCommand, Guid>
{
    /// <summary>성공 시 돌려주는 고정 ID.</summary>
    public static readonly Guid CreatedId = new("0192f3a1-7c4e-7b2a-8d3f-1a2b3c4d5e6f");

    /// <inheritdoc />
    public Task<Result<Guid>> Handle(CreateSampleCommand command, CancellationToken cancellationToken)
    {
        probe.Handling(command);
        var result = command.Value == 0 ? Result.Failure<Guid>(SampleErrors.ValueConflict) : Result.Success(CreatedId);
        return Task.FromResult(result);
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 FluentValidation 검색(includeInternalTypes)으로 등록해 DI가 만든다(테스트 대상 동작).")]
internal sealed class CreateSampleCommandValidator : RequestValidator<CreateSampleCommand>
{
    public CreateSampleCommandValidator(IPipelineProbe probe)
    {
        RuleFor(command => command.Value)
            .Must(value =>
            {
                probe.Validating(value);
                return value >= 0;
            })
            .WithError(SampleErrors.ValueNegative);
    }
}

/// <summary>반환 값이 없는 Command 예시(한 인자 Handler 편의 인터페이스로 구현).</summary>
/// <param name="Value">값.</param>
public sealed record ArchiveSampleCommand(int Value) : ICommand;

/// <summary><see cref="ArchiveSampleCommand"/> Handler. 한 인자 형태 <see cref="ICommandHandler{TCommand}"/>로 구현한다.</summary>
public sealed class ArchiveSampleCommandHandler(IPipelineProbe probe) : ICommandHandler<ArchiveSampleCommand>
{
    /// <inheritdoc />
    public Task<Result<Unit>> Handle(ArchiveSampleCommand command, CancellationToken cancellationToken)
    {
        probe.Handling(command);
        return Task.FromResult(Result.Success(Unit.Value));
    }
}

/// <summary>Query 예시. <see cref="Value"/>가 음수면 검증 실패.</summary>
/// <param name="Value">값.</param>
public sealed record GetSampleQuery(int Value) : IQuery<string>;

/// <summary><see cref="GetSampleQuery"/> Handler.</summary>
public sealed class GetSampleQueryHandler(IPipelineProbe probe) : IQueryHandler<GetSampleQuery, string>
{
    /// <inheritdoc />
    public Task<Result<string>> Handle(GetSampleQuery query, CancellationToken cancellationToken)
    {
        probe.Handling(query);
        return Task.FromResult(Result.Success($"sample-{query.Value}"));
    }
}

/// <summary><see cref="GetSampleQuery"/> Validator.</summary>
public sealed class GetSampleQueryValidator : RequestValidator<GetSampleQuery>
{
    /// <summary>규칙을 정의합니다.</summary>
    /// <param name="probe">실행 기록 탐침.</param>
    public GetSampleQueryValidator(IPipelineProbe probe)
    {
        RuleFor(query => query.Value)
            .Must(value =>
            {
                probe.Validating(value);
                return value >= 0;
            })
            .WithError(SampleErrors.ValueNegative);
    }
}

/// <summary>검색 대상 어셈블리에 Handler가 없는 Command(중복 Handler 시나리오에서 동적 어셈블리가 구현).</summary>
/// <param name="Value">값.</param>
public sealed record OrphanSampleCommand(int Value) : ICommand;
