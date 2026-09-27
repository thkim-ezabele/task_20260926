using EmergencyHub.BuildingBlocks.Application.Cqrs;

namespace EmergencyHub.BuildingBlocks.Application.UnitTests.Samples;

/// <summary>개인정보(이름 · 이메일)를 담은 Command 예시. 로그에 값이 남지 않는지 확인한다.</summary>
public sealed record RegisterPersonCommand(string Name, string Email) : ICommand<Guid>;
