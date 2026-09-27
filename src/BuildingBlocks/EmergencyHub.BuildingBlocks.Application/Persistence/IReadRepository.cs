namespace EmergencyHub.BuildingBlocks.Application.Persistence;

/// <summary>
/// Read Repository 인터페이스의 마커입니다. 서비스 Application의 Read Repository 인터페이스가 상속하고,
/// DI 자동 등록이 이 마커로 구현 타입을 찾아 Scoped로 등록합니다(ADR-0009, ADR-0010, ADR-0017).
/// </summary>
/// <remarks>Query Handler는 이 마커를 상속한 Read Repository로만 조회합니다(ADR-0007).</remarks>
public interface IReadRepository;
