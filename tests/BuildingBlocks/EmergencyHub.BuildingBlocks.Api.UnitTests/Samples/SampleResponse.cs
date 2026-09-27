namespace EmergencyHub.BuildingBlocks.Api.UnitTests.Samples;

/// <summary>샘플 Controller 응답 모델.</summary>
public sealed record SampleResponse(SampleStatus Status, SampleChannels Channels, string? Note);
