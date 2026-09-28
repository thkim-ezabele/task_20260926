using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;
using EmergencyHub.Employee.Domain.Employees;
using FluentValidation.Results;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Commands.RegisterEmployees;

// S06-T04 일괄 등록 Validator(PRD-002 FR-05 · FR-06, ADR-0026 7절): 겉모양만 본다. 판정 순서(첫 실패 하나에서 멈춤, S06 인계 메모):
// 빈 입력 21028(Sources None 또는 Content가 비었거나 BOM 뒤 0x20 · 0x09 · 0x0D · 0x0A만) → Sources 정의 안 된 비트 1002 →
// Sources 두 비트 이상 21029 → Format 1002(빈 입력이 아닐 때만). 21028 · 21029는 요청 전체 오류라 PropertyName "".
// enum 1002 증빙(PRD-001 증빙 테스트 대응표 1002 행)은 이 테스트가 대신한다(새 API에는 사용자가 enum을 직접 보내는 경로가 없음).
[Trait("FR", "PRD-002/FR-06")]
[Trait("FR", "PRD-002/FR-05")]
public sealed class RegisterEmployeesCommandValidatorTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] CsvRow = Encoding.UTF8.GetBytes("홍길동,hong@example.com,010-1234-5678,2020-01-02");

    private readonly RegisterEmployeesCommandValidator _validator = new();

    public static TheoryData<EmployeeImportSources> SingleSources() => new(
        EmployeeImportSources.File, EmployeeImportSources.Data, EmployeeImportSources.Body);

    public static TheoryData<EmployeeImportFormat> DefinedFormats() => new(EmployeeImportFormat.Csv, EmployeeImportFormat.Json);

    public static TheoryData<byte[]> BlankContents() => new(
        [],
        [0xEF, 0xBB, 0xBF],
        [0x20],
        [0x09, 0x0D, 0x0A],
        [0x20, 0x09, 0x0D, 0x0A, 0x20],
        [0xEF, 0xBB, 0xBF, 0x20, 0x09, 0x0D, 0x0A]);

    // 공백 집합(0x20 · 0x09 · 0x0D · 0x0A) 밖의 공백류 · 두 번째 BOM · 공백 뒤 BOM · 잘린 BOM은 빈 입력이 아니다(해독 · 파서가 판정).
    public static TheoryData<byte[]> NonBlankWhitespaceLikeContents() => new(
        [0x0B],
        [0x0C],
        [0x00],
        [0xC2, 0xA0],
        [0xE3, 0x80, 0x80],
        [0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF],
        [0x20, 0xEF, 0xBB, 0xBF],
        [0xEF, 0xBB],
        [0x20, 0x0A, 0x7B]);

    public static TheoryData<EmployeeImportSources> UndefinedSourceBits() => new(
        (EmployeeImportSources)8,
        EmployeeImportSources.File | (EmployeeImportSources)8,
        EmployeeImportSources.File | EmployeeImportSources.Data | (EmployeeImportSources)16,
        (EmployeeImportSources)(-1),
        (EmployeeImportSources)int.MinValue);

    public static TheoryData<EmployeeImportSources> MultipleSources() => new(
        EmployeeImportSources.File | EmployeeImportSources.Data,
        EmployeeImportSources.File | EmployeeImportSources.Body,
        EmployeeImportSources.Data | EmployeeImportSources.Body,
        EmployeeImportSources.File | EmployeeImportSources.Data | EmployeeImportSources.Body);

    public static TheoryData<EmployeeImportFormat> UndefinedFormats() => new(
        EmployeeImportFormat.Unknown, (EmployeeImportFormat)3, (EmployeeImportFormat)(-1), (EmployeeImportFormat)short.MaxValue);

    // ---- 성공 ----

    [Theory]
    [MemberData(nameof(SingleSources))]
    public void Validate_SingleSourceWithCsvContent_IsValid(EmployeeImportSources sources)
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, sources, CsvRow));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DefinedFormats))]
    public void Validate_DefinedFormat_IsValidWithoutLookingAtContentSyntax(EmployeeImportFormat format)
    {
        // 겉모양만 본다: 형식과 맞지 않거나 깨진 내용도 Validator는 통과시킨다(파서 몫).
        var result = _validator.Validate(Command(format, EmployeeImportSources.Body, Encoding.UTF8.GetBytes("[{,")));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BomFollowedByContent_IsValid()
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, EmployeeImportSources.File, [.. Bom, .. CsvRow]));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NonBlankWhitespaceLikeContents))]
    public void Validate_WhitespaceLikeBytesOutsideBlankSet_IsNotEmptyInput(byte[] content)
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, EmployeeImportSources.Data, content));

        result.IsValid.Should().BeTrue();
    }

    // ---- 실패: 빈 입력 21028 ----

    [Fact]
    public void Validate_NoSourceWithContent_ReturnsInputEmptyAtRequestPath()
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, EmployeeImportSources.None, CsvRow));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    [Theory]
    [MemberData(nameof(BlankContents))]
    public void Validate_BlankContent_ReturnsInputEmptyAtRequestPath(byte[] content)
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, EmployeeImportSources.Body, content));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    // ---- 실패: Sources 정의 안 된 비트 1002 ----

    [Theory]
    [MemberData(nameof(UndefinedSourceBits))]
    public void Validate_UndefinedSourceBits_ReturnsInvalidCodeOnSources(EmployeeImportSources sources)
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, sources, CsvRow));

        ShouldHaveSingleError(result, nameof(RegisterEmployeesCommand.Sources), CommonErrors.InvalidCode);
    }

    // ---- 실패: 출처 둘 이상 21029 ----

    [Theory]
    [MemberData(nameof(MultipleSources))]
    public void Validate_MultipleSources_ReturnsMultipleSourcesAtRequestPath(EmployeeImportSources sources)
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, sources, CsvRow));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportMultipleSources);
    }

    // ---- 실패: Format 1002 ----

    [Theory]
    [MemberData(nameof(UndefinedFormats))]
    public void Validate_UndefinedFormatWithContent_ReturnsInvalidCodeOnFormat(EmployeeImportFormat format)
    {
        var result = _validator.Validate(Command(format, EmployeeImportSources.File, CsvRow));

        ShouldHaveSingleError(result, nameof(RegisterEmployeesCommand.Format), CommonErrors.InvalidCode);
    }

    // ---- 엣지: 순서 경계마다 앞 규칙 하나만 보고 ----

    [Fact]
    public void Validate_NoSourceUnknownFormatAndEmptyContent_ReportsOnlyInputEmpty()
    {
        // 입력이 비어 형식을 정하지 못한 요청(Format 0)은 1002가 아니라 21028 하나다(ADR-0026 2절).
        var result = _validator.Validate(Command(EmployeeImportFormat.Unknown, EmployeeImportSources.None, []));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    [Fact]
    public void Validate_BlankContentWithUndefinedSourceBits_ReportsInputEmptyBeforeInvalidCode()
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Csv, (EmployeeImportSources)8, Bom));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    [Fact]
    public void Validate_BlankContentWithMultipleSources_ReportsInputEmptyBeforeMultipleSources()
    {
        var result = _validator.Validate(
            Command(EmployeeImportFormat.Csv, EmployeeImportSources.File | EmployeeImportSources.Data, [0x20, 0x0A]));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    [Fact]
    public void Validate_UndefinedBitsTogetherWithMultipleSources_ReportsInvalidCodeBeforeMultipleSources()
    {
        var result = _validator.Validate(
            Command(EmployeeImportFormat.Csv, EmployeeImportSources.File | EmployeeImportSources.Data | (EmployeeImportSources)8, CsvRow));

        ShouldHaveSingleError(result, nameof(RegisterEmployeesCommand.Sources), CommonErrors.InvalidCode);
    }

    [Fact]
    public void Validate_UndefinedSourceBitsAndUndefinedFormat_ReportsOnlySourcesInvalidCode()
    {
        var result = _validator.Validate(Command(EmployeeImportFormat.Unknown, (EmployeeImportSources)8, CsvRow));

        ShouldHaveSingleError(result, nameof(RegisterEmployeesCommand.Sources), CommonErrors.InvalidCode);
    }

    [Fact]
    public void Validate_MultipleSourcesAndUndefinedFormat_ReportsMultipleSourcesBeforeFormat()
    {
        var result = _validator.Validate(
            Command(EmployeeImportFormat.Unknown, EmployeeImportSources.File | EmployeeImportSources.Data, CsvRow));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportMultipleSources);
    }

    [Fact]
    public void Validate_ContentSlice_UsesOnlyTheGivenRange()
    {
        // ReadOnlyMemory 조각의 범위 밖 바이트는 보지 않는다(바인더가 버퍼 일부를 넘기는 경우).
        byte[] buffer = [.. CsvRow, 0x20, 0x20];
        var blankSlice = new ReadOnlyMemory<byte>(buffer, CsvRow.Length, 2);

        var result = _validator.Validate(new RegisterEmployeesCommand(EmployeeImportFormat.Csv, EmployeeImportSources.Body, blankSlice));

        ShouldHaveSingleError(result, string.Empty, EmployeeErrors.ImportInputEmpty);
    }

    [Fact]
    public void Validate_Always_DoesNotModifyContent()
    {
        byte[] content = [.. Bom, 0x20];
        byte[] original = [.. content];

        _validator.Validate(Command(EmployeeImportFormat.Csv, EmployeeImportSources.Body, content));

        content.Should().Equal(original);
    }

    private static RegisterEmployeesCommand Command(EmployeeImportFormat format, EmployeeImportSources sources, byte[] content) =>
        new(format, sources, content);

    private static void ShouldHaveSingleError(ValidationResult result, string propertyName, Error error)
    {
        var failure = result.Errors.Should().ContainSingle().Subject;
        failure.PropertyName.Should().Be(propertyName);
        failure.CustomState.Should().BeSameAs(error);
        failure.ErrorMessage.Should().Be(error.Message);
    }
}
