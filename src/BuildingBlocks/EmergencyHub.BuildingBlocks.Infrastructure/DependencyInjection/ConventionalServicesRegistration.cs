namespace EmergencyHub.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// <see cref="ConventionalServiceCollectionExtensions.AddConventionalServices"/>가 이미 호출됐음을 서비스 컬렉션에 남기는 표식입니다.
/// </summary>
/// <remarks>
/// 두 번째 호출은 이미 감싼 Handler를 다시 감싸(트랜잭션 · 로깅 두 겹) 커밋이 두 번 일어나므로, 이 표식으로 두 번째 호출을 막습니다.
/// </remarks>
internal sealed class ConventionalServicesRegistration
{
    public static readonly ConventionalServicesRegistration Instance = new();

    private ConventionalServicesRegistration()
    {
    }
}
