using EmergencyHub.BuildingBlocks.Application.Exceptions;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

/// <summary>
/// EF Core 형식을 알아야 하는 예외 분류의 Infrastructure 구현입니다(ADR-0024 "Infrastructure 예외 분류 경로", database.md 규칙표 8).
/// </summary>
/// <remarks>
/// 실행 전략의 재시도 한도 초과(<see cref="RetryLimitExceededException"/>)만 9003으로 분류하고 그 밖은 <see langword="null"/>입니다(전역 예외 처리기가 9001).
/// 예외 자신만 형식으로 검사합니다(안쪽 예외 · 형식 이름 문자열은 보지 않음). 상태가 없어 Singleton으로 등록합니다(<c>AddBuildingBlocksInfrastructure</c>).
/// </remarks>
internal sealed class PersistenceExceptionClassifier : IExceptionClassifier
{
    public Error? Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is RetryLimitExceededException ? CommonErrors.TemporarilyUnavailable : null;
    }
}
