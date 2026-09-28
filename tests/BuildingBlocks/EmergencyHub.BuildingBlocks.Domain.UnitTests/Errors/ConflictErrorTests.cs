using EmergencyHub.BuildingBlocks.Domain.Errors;

namespace EmergencyHub.BuildingBlocks.Domain.UnitTests.Errors;

// ADR-0028 "상세 Conflict 오류": 대표 오류는 호출한 쪽의 Conflict 유형 Error, 상세 항목은 하나 이상 · null 금지 · 복사 · 순서 유지,
// 동등성은 대표 오류와 상세 항목(같은 순서)이 같을 때(ValidationError와 같은 규칙).
public sealed class ConflictErrorTests
{
    private static readonly Error DuplicateEmail = Error.Conflict(23001, "이미 등록된 이메일입니다.");

    private static readonly ConflictDetail Row3 = ConflictDetail.Create("Rows[3].Email", DuplicateEmail);

    private static readonly ConflictDetail Row7 = ConflictDetail.Create("Rows[7].Email", DuplicateEmail);

    public static TheoryData<Error> NonConflictErrors() =>
        new(CommonErrors.ValidationFailed, CommonErrors.PayloadTooLarge, CommonErrors.UnsupportedMediaType, CommonErrors.NotFound, CommonErrors.Unexpected);

    // ---- 성공 ----

    [Fact]
    public void Create_WithConflictErrorAndDetails_UsesRepresentativeCodeMessageAndType()
    {
        var error = ConflictError.Create(DuplicateEmail, [Row3]);

        error.Code.Should().Be(23001);
        error.Message.Should().Be(DuplicateEmail.Message);
        error.Type.Should().Be(ErrorType.Conflict);
        error.Details.Should().Equal(Row3);
    }

    [Fact]
    public void Create_WithDetails_IsAnError()
    {
        var error = ConflictError.Create(DuplicateEmail, [Row3]);

        error.Should().BeAssignableTo<Error>();
    }

    [Fact]
    public void Create_WithMultipleDetails_KeepsOrderAndDuplicates()
    {
        var error = ConflictError.Create(DuplicateEmail, [Row7, Row3, Row7]);

        error.Details.Should().Equal(Row7, Row3, Row7);
    }

    [Fact]
    public void Equals_WithSameRepresentativeAndDetails_ReturnsTrue()
    {
        var left = ConflictError.Create(DuplicateEmail, [Row3, Row7]);
        var right = ConflictError.Create(DuplicateEmail, [Row3, Row7]);

        var result = left.Equals(right);

        result.Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    // ---- 실패 ----

    [Fact]
    public void Create_WithNullError_ThrowsArgumentNullException()
    {
        var act = () => ConflictError.Create(null!, [Row3]);

        act.Should().Throw<ArgumentNullException>().WithParameterName("error");
    }

    [Theory]
    [MemberData(nameof(NonConflictErrors))]
    public void Create_WithNonConflictRepresentative_ThrowsArgumentException(Error nonConflictError)
    {
        var act = () => ConflictError.Create(nonConflictError, [Row3]);

        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void Create_WithNullDetails_ThrowsArgumentNullException()
    {
        var act = () => ConflictError.Create(DuplicateEmail, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("details");
    }

    [Fact]
    public void Create_WithEmptyDetails_ThrowsArgumentException()
    {
        var act = () => ConflictError.Create(DuplicateEmail, []);

        act.Should().Throw<ArgumentException>().WithParameterName("details");
    }

    [Fact]
    public void Create_WithNullDetailElement_ThrowsArgumentException()
    {
        var act = () => ConflictError.Create(DuplicateEmail, [Row3, null!]);

        act.Should().Throw<ArgumentException>().WithParameterName("details");
    }

    [Fact]
    public void Equals_WithDifferentDetailOrder_ReturnsFalse()
    {
        var left = ConflictError.Create(DuplicateEmail, [Row3, Row7]);
        var right = ConflictError.Create(DuplicateEmail, [Row7, Row3]);

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentRepresentative_ReturnsFalse()
    {
        var left = ConflictError.Create(DuplicateEmail, [Row3]);
        var right = ConflictError.Create(CommonErrors.UniqueConstraintViolated, [Row3]);

        var result = left.Equals(right);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithPlainRepresentativeErrorOrNull_ReturnsFalse()
    {
        var error = ConflictError.Create(DuplicateEmail, [Row3]);

        error.Equals(DuplicateEmail).Should().BeFalse();
        DuplicateEmail.Equals(error).Should().BeFalse();
        error.Equals(null).Should().BeFalse();
    }

    // ---- 엣지 ----

    [Fact]
    public void Create_WhenSourceListChangesAfterward_KeepsOriginalDetails()
    {
        var source = new List<ConflictDetail> { Row3 };
        var error = ConflictError.Create(DuplicateEmail, source);

        source.Add(Row7);

        error.Details.Should().Equal(Row3);
    }

    [Fact]
    public void Details_WhenCastToMutableList_ThrowsNotSupportedException()
    {
        var error = ConflictError.Create(DuplicateEmail, [Row3]);
        var list = (IList<ConflictDetail>)error.Details;

        var act = () => list[0] = Row7;

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Create_WithDetailCodeDifferentFromRepresentative_KeepsEachCode()
    {
        // 대표 코드와 상세 코드는 따로다(예: 대표 23001, 상세 3003). 둘 다 Conflict 유형이면 된다.
        var unmapped = ConflictDetail.Create("Rows[2].Email", CommonErrors.UniqueConstraintViolated);

        var error = ConflictError.Create(DuplicateEmail, [Row3, unmapped]);

        error.Code.Should().Be(23001);
        error.Details.Select(detail => detail.Code).Should().Equal(23001, 3003);
    }
}
