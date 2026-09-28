namespace EmergencyHub.Employee.Api.UnitTests.TestDoubles;

/// <summary>읽으면 실패하는 본문입니다. 본문을 읽기 전에 거절하는지 확인합니다(<see cref="ReadCount"/>).</summary>
internal sealed class ThrowingStream : Stream
{
    /// <summary>읽기를 시도한 횟수.</summary>
    public int ReadCount { get; private set; }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCount++;
        throw new InvalidOperationException("본문을 읽으면 안 된다.");
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        throw new InvalidOperationException("본문을 읽으면 안 된다.");
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
