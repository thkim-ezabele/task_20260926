using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks>서비스 Infrastructure가 하는 23505 매핑 등록 예시(키는 이름 상수 참조, 문자열 리터럴 금지).</remarks>
public static class SampleUniqueConstraintErrors
{
    public static void Configure(UniqueConstraintErrorsBuilder errors) =>
        errors.Map(SampleIndexNames.OrdersOrderNumber, SampleErrors.ValueConflict);

    public static UniqueConstraintErrorRegistry Create()
    {
        var builder = new UniqueConstraintErrorsBuilder();
        Configure(builder);
        return builder.Build();
    }
}
