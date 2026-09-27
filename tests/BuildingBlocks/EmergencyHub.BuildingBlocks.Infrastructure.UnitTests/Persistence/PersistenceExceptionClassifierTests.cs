using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Exceptions;
using EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Samples.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EmergencyHub.BuildingBlocks.Infrastructure.UnitTests.Persistence;

// ADR-0024 예외 분류 포트의 Infrastructure 구현: RetryLimitExceededException → 9003, 그 밖은 null(전역 예외 처리기가 9001).
// 판정은 형식 검사(is)이고 형식 이름 문자열을 비교하지 않는다. database.md 규칙표 8.
public sealed class PersistenceExceptionClassifierTests
{
    private readonly PersistenceExceptionClassifier _classifier = new();

    [Fact]
    public void Classify_RetryLimitExceeded_ReturnsTemporarilyUnavailable()
    {
        _classifier.Classify(new RetryLimitExceededException("retry limit")).Should().Be(CommonErrors.TemporarilyUnavailable);
    }

    [Fact]
    public void Classify_RetryLimitExceededWithTransientInner_ReturnsTemporarilyUnavailable()
    {
        var exception = new RetryLimitExceededException("retry limit", SamplePostgresExceptions.Create(PostgresErrorCodes.SerializationFailure));

        _classifier.Classify(exception).Should().Be(CommonErrors.TemporarilyUnavailable);
    }

    [Fact]
    public void Classify_CheckViolationDbUpdateException_ReturnsNull()
    {
        var exception = SamplePostgresExceptions.Wrap(SamplePostgresExceptions.Create(PostgresErrorCodes.CheckViolation, "ck_orders_order_status"));

        _classifier.Classify(exception).Should().BeNull();
    }

    [Fact]
    public void Classify_TransientPostgresExceptionNotWrappedInRetryLimit_ReturnsNull()
    {
        _classifier.Classify(SamplePostgresExceptions.Create(PostgresErrorCodes.SerializationFailure)).Should().BeNull();
    }

    [Fact]
    public void Classify_ExceptionWrappingRetryLimitExceeded_ReturnsNullBecauseOnlyTheExceptionItselfIsChecked()
    {
        var wrapped = new InvalidOperationException("wrapper", new RetryLimitExceededException("retry limit"));

        _classifier.Classify(wrapped).Should().BeNull();
    }

    [Fact]
    public void Classify_OperationCanceled_ReturnsNull()
    {
        _classifier.Classify(new OperationCanceledException()).Should().BeNull();
    }

    [Fact]
    public void Classify_Null_ThrowsArgumentNullException()
    {
        var act = () => _classifier.Classify(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
