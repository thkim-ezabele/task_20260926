using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;

namespace EmergencyHub.ArchitectureTests.Samples.ApplicationInfrastructureDependencies;

/// <summary>위반 예: 메서드 본문에서만 BuildingBlocks.Infrastructure 형식을 쓴다.</summary>
/// <remarks>
/// <c>const</c> 필드(예: <c>ShadowPropertyNames.CreatedAt</c>) 참조는 컴파일러가 값을 인라인해 IL에 형식 참조가 남지 않으므로
/// 형식 의존 규칙이 잡지 못한다(런타임 의존도 생기지 않음, S02-T05 실측). 그래서 이 표본은 형식을 실제로 만든다.
/// </remarks>
public sealed class IndexNameApplicationType
{
    /// <summary>표본 메서드.</summary>
    /// <returns>고유 인덱스 이름 문자열.</returns>
    public static string IndexName() => new UniqueIndexName("ux_samples_name").Value;
}
