using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary><see cref="ChannelFilterQuery"/> Handler. 받은 값을 그대로 돌려줍니다.</summary>
public sealed class ChannelFilterQueryHandler : IQueryHandler<ChannelFilterQuery, SampleResponse>
{
    /// <inheritdoc />
    public Task<Result<SampleResponse>> Handle(ChannelFilterQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return Task.FromResult(Result.Success(new SampleResponse(query.Status, query.Channels, Note: null)));
    }
}
