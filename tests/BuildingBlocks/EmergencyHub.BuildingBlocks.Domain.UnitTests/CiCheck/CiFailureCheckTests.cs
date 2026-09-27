using EmergencyHub.BuildingBlocks.Domain.Results;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.CiCheck;

public sealed class CiFailureCheckTests
{
    [Fact]
    public void Success_Always_FailsIntentionallyForCiFailureCheck()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeFalse("S04-T06 CI 실패 표시 확인용 의도된 단언 실패입니다");
    }
}
