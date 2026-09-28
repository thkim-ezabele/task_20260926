using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace EmergencyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// 서비스 DbContext의 모델 정의입니다. 쓰기 · 읽기 DbContext가 <b>같은 인스턴스</b>를 돌려주어 두 관계형 모델이 같게 만듭니다.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WriteDbContextBase"/> · <see cref="ReadDbContextBase"/>가 <c>ConfigureConventions</c> · <c>OnModelCreating</c>을 봉인하고
/// 이 정의와 공통 규칙(snake_case 뒤처리, 강타입 ID 변환, 도메인 이벤트 제외, 감사 · <c>xmin</c> shadow property, <c>ck_</c>)을 같은 순서로 적용합니다
/// (database.md "EF Core 공통 모델 규칙").
/// </para>
/// <para>구현은 서비스 Infrastructure에 하나 두고 두 DbContext가 공유합니다. 마커를 상속하지 않으므로 DI 자동 등록 대상이 아닙니다.</para>
/// </remarks>
public interface IDbModelDefinition
{
    /// <summary>
    /// <c>IStronglyTypedId&lt;TSelf&gt;</c> 구현 형식을 찾을 어셈블리입니다(보통 서비스 Domain). 찾은 형식마다 <c>uuid</c> 값 변환기를 등록합니다.
    /// </summary>
    IReadOnlyCollection<Assembly> StronglyTypedIdAssemblies { get; }

    /// <summary>
    /// 엔티티 매핑을 적용합니다. 보통 <c>modelBuilder.ApplyConfigurationsFromAssembly(...)</c> 한 줄입니다.
    /// 공통 규칙의 뒤처리(감사 · 동시성 토큰 · 키 값 생성)는 이 메서드가 끝난 뒤에 적용됩니다.
    /// </summary>
    /// <param name="modelBuilder">모델 빌더.</param>
    void ConfigureModel(ModelBuilder modelBuilder);
}
