namespace EmergencyHub.BuildingBlocks.Domain.Repositories;

/// <summary>
/// Write Repository 인터페이스의 마커입니다. 서비스 Domain의 Repository 인터페이스가 상속하고,
/// DI 자동 등록이 이 마커로 구현 타입을 찾아 Scoped로 등록합니다(ADR-0010, ADR-0017).
/// </summary>
public interface IRepository;
