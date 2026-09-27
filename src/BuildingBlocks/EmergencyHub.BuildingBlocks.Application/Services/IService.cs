namespace EmergencyHub.BuildingBlocks.Application.Services;

/// <summary>
/// 서비스 인터페이스(외부 연동 어댑터 등)의 마커입니다. DI 자동 등록이 이 마커로 구현 타입을 찾아 Scoped로 등록합니다(ADR-0010, ADR-0017).
/// </summary>
/// <remarks>
/// 구현 클래스는 이 마커를 상속한 서비스 인터페이스를 하나만 구현합니다. 같은 인터페이스를 두 구현이 등록하면 시작 시 실패합니다.
/// </remarks>
public interface IService;
