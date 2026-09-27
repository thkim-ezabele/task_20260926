using Microsoft.AspNetCore.Http.Features;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>응답이 이미 시작된 상태(<see cref="IHttpResponseFeature.HasStarted"/>)를 흉내 내는 기능입니다.</summary>
internal sealed class StartedResponseFeature : HttpResponseFeature
{
    public override bool HasStarted => true;
}
