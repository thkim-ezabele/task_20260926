using System.Data;
using System.Data.Common;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;

/// <remarks><see cref="FakeDatabase"/>가 트랜잭션 시작을 건너뛸 때 돌려주는 트랜잭션. Dispose(롤백 또는 커밋 뒤 정리)를 기록한다.</remarks>
public sealed class FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel, FakeDatabase database) : DbTransaction
{
    public override IsolationLevel IsolationLevel => isolationLevel;

    protected override DbConnection DbConnection => connection;

    public override void Commit() => database.Record("DbCommit");

    public override void Rollback() => database.Record("Rollback");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            database.Record("Dispose");
        }

        base.Dispose(disposing);
    }
}
