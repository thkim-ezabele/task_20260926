namespace EmergencyHub.Employee.IntegrationTests.Fixtures;

/// <summary>
/// Employee DB를 쓰는 테스트 컬렉션입니다. 컨테이너 1개(<see cref="EmployeeDatabaseFixture"/>)를 공유하고 컬렉션 안에서 순차 실행합니다
/// (TRUNCATE의 ACCESS EXCLUSIVE 잠금, 테스트 전용 트리거). S03-T07 HTTP 통합 테스트도 이 컬렉션을 씁니다.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EmployeeDatabaseCollectionDefinition : ICollectionFixture<EmployeeDatabaseFixture>
{
    /// <summary>컬렉션 이름입니다. 테스트 클래스에 <c>[Collection(EmployeeDatabaseCollectionDefinition.Name)]</c>으로 씁니다.</summary>
    public const string Name = "EmployeeDatabase";
}
