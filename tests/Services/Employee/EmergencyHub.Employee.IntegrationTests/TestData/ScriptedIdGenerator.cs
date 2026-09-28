using EmergencyHub.BuildingBlocks.Application.Identifiers;

namespace EmergencyHub.Employee.IntegrationTests.TestData;

/// <summary>
/// 정해 둔 ID를 순서대로 돌려주는 <see cref="IIdGenerator"/> 대역입니다(S06-T06 같은 ID 재전송: 두 요청이 같은 ID를 받게 함).
/// </summary>
/// <remarks>
/// <see cref="Fixtures.EmployeeApiFactoryOptions.IdGenerator"/>로 싱글턴 등록합니다. 다 쓰면 <see cref="InvalidOperationException"/>을 던지고,
/// <see cref="Rewind"/>로 처음부터 다시 돌려줍니다. 여러 요청이 동시에 불러도 안전합니다.
/// </remarks>
public sealed class ScriptedIdGenerator : IIdGenerator
{
    private readonly Guid[] _ids;
    private readonly object _gate = new();
    private int _next;

    /// <summary>돌려줄 ID 목록으로 만듭니다.</summary>
    /// <param name="ids">순서대로 돌려줄 ID. 비어 있거나 <see cref="Guid.Empty"/>가 있으면 안 됩니다.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/>가 <see langword="null"/>인 경우.</exception>
    /// <exception cref="ArgumentException">목록이 비었거나 <see cref="Guid.Empty"/>가 있는 경우(운영 계약: 빈 값 없음).</exception>
    public ScriptedIdGenerator(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        _ids = [.. ids];
        if (_ids.Length == 0 || _ids.Contains(Guid.Empty))
        {
            throw new ArgumentException("ID 목록은 비어 있지 않고 Guid.Empty가 없어야 합니다.", nameof(ids));
        }
    }

    /// <summary>지금까지 돌려준 ID 개수입니다(<see cref="Rewind"/>하면 0).</summary>
    public int Issued
    {
        get
        {
            lock (_gate)
            {
                return _next;
            }
        }
    }

    /// <summary>임의 ID <paramref name="count"/>개(<see cref="Guid.NewGuid"/>, 값만 다르면 됨)로 만듭니다.</summary>
    /// <param name="count">ID 개수(1 이상).</param>
    /// <returns>대역.</returns>
    public static ScriptedIdGenerator WithRandomIds(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        return new ScriptedIdGenerator(Enumerable.Range(0, count).Select(_ => Guid.NewGuid()));
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">준비한 ID를 다 쓴 경우.</exception>
    public Guid NewId()
    {
        lock (_gate)
        {
            if (_next >= _ids.Length)
            {
                throw new InvalidOperationException($"ScriptedIdGenerator의 ID {_ids.Length}개를 모두 썼습니다. Rewind로 처음부터 다시 쓸 수 있습니다.");
            }

            return _ids[_next++];
        }
    }

    /// <summary>처음 ID부터 다시 돌려줍니다(다음 요청이 앞 요청과 같은 ID를 받음).</summary>
    public void Rewind()
    {
        lock (_gate)
        {
            _next = 0;
        }
    }
}
