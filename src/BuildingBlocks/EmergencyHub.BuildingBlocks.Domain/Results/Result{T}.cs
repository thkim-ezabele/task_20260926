using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.Results;

/// <summary>
/// 값이 있는 연산의 성공 / 실패 결과입니다. 성공이면 <see cref="Value"/>, 실패면 <see cref="Result.Error"/>를 가집니다.
/// </summary>
/// <typeparam name="T">값 형식. 성공 값은 <see langword="null"/>일 수 없습니다.</typeparam>
/// <remarks>
/// 두 방향의 암시적 변환을 제공합니다. <c>return result.Error;</c>(오류 → 실패)와
/// <c>return result.Value.Id;</c>(값 → 성공)를 그대로 쓸 수 있습니다(coding-conventions.md Handler 예시).
/// </remarks>
public sealed class Result<T> : Result
{
    // 실패 결과에서는 기본값이 들어가지만 Value가 실패일 때 읽기를 막으므로 밖으로 새지 않는다.
    [AllowNull]
    private readonly T _value;

    internal Result(T value)
        : base(null)
    {
        _value = value;
    }

    internal Result(Error error)
        : base(error)
    {
        _value = default;
    }

    /// <summary>
    /// 성공 값입니다. <see cref="Result.IsSuccess"/>를 확인한 뒤에 읽습니다.
    /// </summary>
    /// <exception cref="InvalidOperationException">실패한 결과에서 읽은 경우.</exception>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("실패한 결과에는 Value가 없습니다. Error를 확인하세요.");

    /// <summary>
    /// 값을 성공 결과로 바꿉니다.
    /// </summary>
    /// <param name="value">결과 값.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>가 <see langword="null"/>인 경우.</exception>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// 오류를 실패 결과로 바꿉니다.
    /// </summary>
    /// <param name="error">실패 원인.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/>가 <see langword="null"/>인 경우.</exception>
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
