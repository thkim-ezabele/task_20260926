using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>개인정보(이메일)를 조건으로 받는 Query 예시. 로그에 값이 남지 않는지 확인한다.</summary>
public sealed record FindPersonQuery(string Email) : IQuery<SampleResponse>;
