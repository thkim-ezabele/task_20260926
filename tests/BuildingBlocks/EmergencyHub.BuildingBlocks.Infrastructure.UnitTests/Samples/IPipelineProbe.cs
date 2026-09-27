namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// Validator · Handler가 불렸는지와 순서를 기록하는 탐침입니다. 마커를 상속하지 않으므로 테스트가 대역을 직접 등록합니다.
/// </summary>
public interface IPipelineProbe
{
    /// <summary>Validator 규칙이 실행될 때 부릅니다.</summary>
    /// <param name="request">검증 중인 요청.</param>
    void Validating(object request);

    /// <summary>Handler 본문이 실행될 때 부릅니다.</summary>
    /// <param name="request">처리 중인 요청.</param>
    void Handling(object request);
}
