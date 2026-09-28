namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>코드값 규칙(1002) 검증용 요청 예시.</summary>
public sealed record CodeRequest(SampleStatus Status, SampleChannels Channels, WideSampleChannels Wide);
