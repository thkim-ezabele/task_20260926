using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

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
