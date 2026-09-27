using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

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
