using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.Results;

/// <summary>
/// 값이 없는 연산의 성공 / 실패 결과입니다. 예상 가능한 실패는 예외가 아니라 이 타입으로 돌려줍니다.
/// </summary>
/// <remarks>
/// <para>
/// 불변식: 성공이면 <see cref="Error"/>가 없고, 실패면 반드시 <see langword="null"/>이 아닌 <see cref="Errors.Error"/>가 있습니다.
/// 성공은 인자 없는 <see cref="Success()"/>, 실패는 <see cref="Failure(Errors.Error)"/>로만 만들므로
/// "성공인데 오류가 있음 / 실패인데 오류가 없음" 상태는 만들 수 없습니다. 그래서 <c>Error.None</c> 같은 빈 오류 값을 두지 않습니다.
/// </para>
/// <para>
/// "클래스는 기본 <c>sealed</c>" 규칙의 예외로 non-sealed입니다. 값이 있는 결과 <see cref="Result{T}"/>가 파생하며,
/// 생성자가 <c>private protected</c>라 이 어셈블리 밖에서는 파생할 수 없습니다.
/// 동등성이 필요 없는 흐름 제어 타입이라 <c>record</c>가 아니라 클래스입니다.
/// </para>
/// </remarks>
public class Result
{
    private static readonly Result SuccessResult = new(null);

    private readonly Error? _error;

    private protected Result(Error? error)
    {
        _error = error;
    }

    /// <summary>성공이면 <see langword="true"/>입니다.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>실패면 <see langword="true"/>입니다.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>
    /// 실패 원인입니다. <see cref="IsFailure"/>를 확인한 뒤에 읽습니다.
    /// </summary>
    /// <exception cref="InvalidOperationException">성공한 결과에서 읽은 경우.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("성공한 결과에는 Error가 없습니다.");

    /// <summary>
    /// 오류를 실패 결과로 바꿉니다. <c>return SomeErrors.NotFound;</c>처럼 쓸 수 있습니다.
    /// </summary>
    /// <param name="error">실패 원인.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>성공 결과를 돌려줍니다.</summary>
    /// <returns>성공 결과.</returns>
    public static Result Success() => SuccessResult;

    /// <summary>실패 결과를 만듭니다.</summary>
    /// <param name="error">실패 원인.</param>
    /// <returns>실패 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    /// <summary>값이 있는 성공 결과를 만듭니다.</summary>
    /// <typeparam name="TValue">값 형식.</typeparam>
    /// <param name="value">결과 값. <see langword="null"/>일 수 없습니다.</param>
    /// <returns>성공 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>가 <see langword="null"/>인 경우.</exception>
    public static Result<TValue> Success<TValue>(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "성공 결과의 값은 null일 수 없습니다. 대상이 없으면 실패 결과를 돌려주세요.");
        }

        return new Result<TValue>(value);
    }

    /// <summary>값 형식이 있는 실패 결과를 만듭니다.</summary>
    /// <typeparam name="TValue">값 형식.</typeparam>
    /// <param name="error">실패 원인.</param>
    /// <returns>실패 결과.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public static Result<TValue> Failure<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(error);
    }
}
