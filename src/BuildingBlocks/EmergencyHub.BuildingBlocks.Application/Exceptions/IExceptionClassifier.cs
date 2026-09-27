using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Application.Exceptions;

/// <summary>
/// 처리되지 않은 예외를 공통 오류(<see cref="Error"/>)로 분류하는 포트입니다(ADR-0024 "Infrastructure 예외 분류 경로").
/// </summary>
/// <remarks>
/// <para>
/// Infrastructure 형식을 알아야 하는 분류(예: EF Core 재시도 한도 초과 → 9003)를 Infrastructure 구현으로 뒤집기 위해 둡니다.
/// 매개변수는 <see cref="Exception"/>뿐이라 EF Core · Npgsql 예외 형식이 이 계약에 나타나지 않습니다.
/// </para>
/// <para>
/// 전역 예외 처리기(BuildingBlocks.Api)가 등록된 분류기를 차례로 묻고 모두 <see langword="null"/>이면 9001로 응답합니다.
/// 여러 구현을 함께 등록하므로 DI 자동 등록 마커(<c>IService</c>)를 상속하지 않습니다(등록은 S02-T07).
/// 타입 이름 문자열 비교로 형식을 판별하지 않습니다.
/// </para>
/// </remarks>
public interface IExceptionClassifier
{
    /// <summary>예외를 분류합니다.</summary>
    /// <param name="exception">처리되지 않은 예외.</param>
    /// <returns>대응하는 공통 오류. 이 분류기가 알지 못하는 예외면 <see langword="null"/>.</returns>
    Error? Classify(Exception exception);
}
