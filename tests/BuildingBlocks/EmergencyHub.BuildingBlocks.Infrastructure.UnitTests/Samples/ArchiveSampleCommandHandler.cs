using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

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
