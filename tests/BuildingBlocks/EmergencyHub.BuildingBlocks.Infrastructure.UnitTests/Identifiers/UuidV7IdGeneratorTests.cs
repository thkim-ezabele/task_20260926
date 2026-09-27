using System.Collections.Concurrent;
using EmergencyHub.BuildingBlocks.Infrastructure.Identifiers;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Identifiers;

// PRD-001 FR-06 · ADR-0013: UUIDNext Uuid.NewDatabaseFriendly(Database.PostgreSql) 기반 UUID v7.
// 정렬 비교는 문자열("D", Ordinal) 또는 빅 엔디언 바이트 사전식으로 한다. Guid.CompareTo · 기본 ToByteArray()는
// .NET 내부 바이트 순서(앞 세 필드 리틀 엔디언)라 생성 순서와 다를 수 있어 쓰지 않는다.
// UUIDNext는 TimeProvider가 아니라 DateTimeOffset.UtcNow를 쓰므로 타임스탬프는 허용 오차로 확인한다. DB 정렬은 S03-T05.
[Trait("FR", "PRD-001/FR-06")]
public sealed class UuidV7IdGeneratorTests
{
    private const int SequentialCount = 10_000;

    // "D" 형식 xxxxxxxx-xxxx-Mxxx-Nxxx-xxxxxxxxxxxx 에서 버전(M) · variant(N) 문자의 위치.
    private const int VersionCharIndex = 14;
    private const int VariantCharIndex = 19;

    private static readonly TimeSpan TimestampTolerance = TimeSpan.FromSeconds(1);

    private readonly UuidV7IdGenerator _generator = new();

    // ---- 성공: 형식 ----

    [Fact]
    public void NewId_Called_HasVersion7Nibble()
    {
        var id = _generator.NewId();

        id.ToString("D")[VersionCharIndex].Should().Be('7');
    }

    [Fact]
    public void NewId_Called_HasRfc9562VariantBits()
    {
        var id = _generator.NewId();

        // variant 10xx → 16진수 8, 9, a, b
        id.ToString("D")[VariantCharIndex].Should().BeOneOf('8', '9', 'a', 'b');
    }

    [Fact]
    public void NewId_ManyIds_AllHaveVersion7AndRfcVariant()
    {
        var ids = Enumerable.Range(0, 1_000).Select(_ => _generator.NewId().ToString("D")).ToList();

        ids.Should().OnlyContain(id => id[VersionCharIndex] == '7' && "89ab".Contains(id[VariantCharIndex]));
    }

    [Fact]
    public void NewId_Called_Leading48BitsAreUnixMillisecondsOfNowWithinTolerance()
    {
        var before = DateTimeOffset.UtcNow;

        var id = _generator.NewId();

        var after = DateTimeOffset.UtcNow;
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(UnixMilliseconds(id));
        // 하한: 시계 역행 방지 · 카운터 넘침은 시각을 앞으로만 민다. 상한: 넘침 시 다음 밀리초를 빌려 쓰므로 여유를 둔다.
        timestamp.Should().BeOnOrAfter(before.AddMilliseconds(-1));
        timestamp.Should().BeOnOrBefore(after + TimestampTolerance);
    }

    // ---- 성공: 순서 ----

    [Fact]
    public void NewId_SequentialTenThousand_SameMillisecondPairsExistAndStringOrderIsStrictlyIncreasing()
    {
        var ids = Enumerable.Range(0, SequentialCount).Select(_ => _generator.NewId()).ToList();

        // 같은 밀리초 안의 단조성을 검증하려면 같은 밀리초 쌍이 실제로 있어야 한다(없으면 시간 순서만 본 셈).
        var sameMillisecondPairs = ids.Zip(ids.Skip(1)).Count(pair => UnixMilliseconds(pair.First) == UnixMilliseconds(pair.Second));
        sameMillisecondPairs.Should().BePositive();

        var strings = ids.Select(id => id.ToString("D")).ToList();
        strings.Zip(strings.Skip(1)).Should().OnlyContain(pair => string.CompareOrdinal(pair.First, pair.Second) < 0);
    }

    [Fact]
    public void NewId_SequentialTenThousand_OrdinalSortEqualsGenerationOrder()
    {
        var ids = Enumerable.Range(0, SequentialCount).Select(_ => _generator.NewId().ToString("D")).ToList();

        ids.Order(StringComparer.Ordinal).Should().Equal(ids);
    }

    [Fact]
    public void NewId_SequentialTenThousand_BigEndianBytesAreLexicographicallyIncreasing()
    {
        // PostgreSQL uuid는 RFC(빅 엔디언) 바이트로 비교한다. 같은 순서가 나와야 DB 정렬 = 생성 순서다(DB 확인은 S03-T05).
        var bytes = Enumerable.Range(0, SequentialCount).Select(_ => _generator.NewId().ToByteArray(bigEndian: true)).ToList();

        bytes.Zip(bytes.Skip(1)).Should().OnlyContain(pair => CompareBytes(pair.First, pair.Second) < 0);
    }

    [Fact]
    public void NewId_TwoInstancesInterleaved_OrderContinuesAcrossInstances()
    {
        // 생성기 상태는 UUIDNext 정적 생성기에 있으므로 Scoped 인스턴스가 여러 개여도 프로세스 안 순서가 이어진다(ADR-0013).
        var other = new UuidV7IdGenerator();

        var ids = Enumerable.Range(0, 2_000)
            .Select(i => (i % 2 == 0 ? _generator : other).NewId().ToString("D"))
            .ToList();

        ids.Zip(ids.Skip(1)).Should().OnlyContain(pair => string.CompareOrdinal(pair.First, pair.Second) < 0);
    }

    // ---- 실패: 빈 값 · 중복이 나오면 안 된다 ----

    [Fact]
    public void NewId_SequentialTenThousand_NeverReturnsEmptyGuid()
    {
        var ids = Enumerable.Range(0, SequentialCount).Select(_ => _generator.NewId());

        ids.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void NewId_SequentialTenThousand_AllDistinct()
    {
        var ids = Enumerable.Range(0, SequentialCount).Select(_ => _generator.NewId()).ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    // ---- 엣지: 동시 생성 ----

    [Fact]
    public async Task NewId_ParallelFromEightThreads_AllDistinctAndVersion7()
    {
        // 스레드 사이 전역 순서는 보장 대상이 아니므로 중복 없음 · 형식만 확인한다(dba 인계).
        var ids = new ConcurrentBag<Guid>();
        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            var generator = new UuidV7IdGenerator();
            for (var i = 0; i < 5_000; i++)
            {
                ids.Add(generator.NewId());
            }
        }));

        await Task.WhenAll(tasks);

        ids.Should().HaveCount(40_000);
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().OnlyContain(id => id.ToString("D")[VersionCharIndex] == '7');
    }

    [Fact]
    public void NewId_EachThreadSequence_IsStrictlyIncreasingWithinThread()
    {
        // 한 스레드가 연속으로 만든 값은 다른 스레드와 섞여도 자기 순서 안에서 증가한다(프로세스 안 단조성, lock).
        var perThread = new ConcurrentBag<List<string>>();

        Parallel.For(0, 4, _ =>
        {
            var local = new List<string>(2_000);
            for (var i = 0; i < 2_000; i++)
            {
                local.Add(_generator.NewId().ToString("D"));
            }

            perThread.Add(local);
        });

        perThread.Should().HaveCount(4);
        perThread.Should().OnlyContain(list => list.Zip(list.Skip(1)).All(pair => string.CompareOrdinal(pair.First, pair.Second) < 0));
    }

    private static int CompareBytes(byte[] left, byte[] right) => left.AsSpan().SequenceCompareTo(right);

    private static long UnixMilliseconds(Guid id)
    {
        var bytes = id.ToByteArray(bigEndian: true);
        return ((long)bytes[0] << 40) | ((long)bytes[1] << 32) | ((long)bytes[2] << 24) | ((long)bytes[3] << 16) | ((long)bytes[4] << 8) | bytes[5];
    }
}
