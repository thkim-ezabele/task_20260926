using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>
/// 코드값(일반 enum <see cref="SampleStatus"/> : <c>short</c>)과 비트 플래그(<see cref="SampleChannels"/> : <c>int</c>)를 받는 Query 예시입니다.
/// 정의되지 않은 enum 정수가 검증 파이프라인을 거쳐 1002로 응답되는지 확인합니다(FR-07).
/// </summary>
/// <param name="Status">상태 코드값.</param>
/// <param name="Channels">알림 채널 조합.</param>
public sealed record ChannelFilterQuery(SampleStatus Status, SampleChannels Channels) : IQuery<SampleResponse>;
