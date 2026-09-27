namespace EmergencyHub.BuildingBlocks.Application.Cqrs;

/// <summary>
/// 반환 값이 없는 Command입니다. <c>ICommand : ICommand&lt;Unit&gt;</c>이므로 결과는 <c>Result&lt;Unit&gt;</c>입니다(ADR-0015).
/// </summary>
public interface ICommand : ICommand<Unit>;
