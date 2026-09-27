namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples;

/// <summary>
/// 여러 스레드에서 동시에 불려도 호출 수를 잃지 않는 <see cref="IPipelineProbe"/> 구현입니다(동시 Send 인수 테스트용).
/// </summary>
/// <remarks>마커를 상속하지 않으므로 자동 등록되지 않고, 테스트가 직접 등록합니다.</remarks>
public sealed class CountingPipelineProbe : IPipelineProbe
{
    private int _validated;
    private int _handled;

    /// <summary>Validator 규칙이 실행된 횟수.</summary>
    public int Validated => Volatile.Read(ref _validated);

    /// <summary>Handler 본문이 실행된 횟수.</summary>
    public int Handled => Volatile.Read(ref _handled);

    /// <inheritdoc />
    public void Validating(object request) => Interlocked.Increment(ref _validated);

    /// <inheritdoc />
    public void Handling(object request) => Interlocked.Increment(ref _handled);
}
