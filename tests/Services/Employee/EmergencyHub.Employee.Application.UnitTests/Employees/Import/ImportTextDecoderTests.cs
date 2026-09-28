using EmergencyHub.Employee.Application.Employees.Import;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.UnitTests.Employees.Import;

// 원본: ADR-0026 3절(엄격 UTF-8 해독은 파서 앞 공통 단계, BOM 허용, 잘못된 바이트 → 21022), PRD-002 FR-03.
public sealed class ImportTextDecoderTests
{
    // 잘못된 UTF-8 바이트열. 바이트 배열이라 문자열 직렬화 문제는 없지만 BL-131 규칙대로 발견 단계 열거를 끄고 입력 보존을 단언한다.
    public static TheoryData<string, byte[]> InvalidUtf8 => new()
    {
        { "CP949 김이름", [0xB1, 0xE8, 0xC0, 0xCC, 0xB8, 0xA7] },
        { "UTF-8로 인코딩한 서로게이트 U+D800", [0xED, 0xA0, 0x80] },
        { "UTF-8로 인코딩한 서로게이트 U+DFFF", [0x41, 0xED, 0xBF, 0xBF, 0x42] },
        { "과잉 길이 NUL", [0xC0, 0x80] },
        { "짝 없는 연속 바이트", [0x80] },
        { "UTF-8에 없는 바이트 FF", [0xFF] },
        { "끝에서 잘린 3바이트 문자", [0x41, 0xEA, 0xB9] },
        { "U+10FFFF 초과", [0xF4, 0x90, 0x80, 0x80] },
    };

    // ---- 성공 ----

    [Fact]
    public void Decode_ValidUtf8_ReturnsText()
    {
        var result = ImportTextDecoder.Decode("김이름,kim@gmail.com"u8);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("김이름,kim@gmail.com");
    }

    [Fact]
    public void Decode_FourByteCharacter_ReturnsSurrogatePair()
    {
        var result = ImportTextDecoder.Decode([0xF0, 0x9F, 0x98, 0x80]);

        result.Value.Should().Be("\U0001F600");
    }

    [Fact]
    public void Decode_LeadingBom_StripsBom()
    {
        var result = ImportTextDecoder.Decode([0xEF, 0xBB, 0xBF, 0x41]);

        result.Value.Should().Be("A");
    }

    // ---- 실패 ----

    [Theory]
    [MemberData(nameof(InvalidUtf8), DisableDiscoveryEnumeration = true)]
    public void Decode_InvalidUtf8_ReturnsImportInvalidUtf8(string description, byte[] content)
    {
        description.Should().NotBeEmpty();
        content.Should().NotBeEmpty("테스트 데이터가 직렬화로 바뀌지 않고 바이트를 담아야 한다");

        var result = ImportTextDecoder.Decode(content);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Decode_InvalidBytesAfterBom_ReturnsImportInvalidUtf8()
    {
        var result = ImportTextDecoder.Decode([0xEF, 0xBB, 0xBF, 0xED, 0xA0, 0x80]);

        result.Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Decode_InvalidUtf8_MessageDoesNotContainInput()
    {
        // NFR-04: 오류 메시지에 입력 값이 없다.
        var result = ImportTextDecoder.Decode([0x6B, 0x69, 0x6D, 0xFF]);

        result.Error.Message.Should().NotContain("kim");
    }

    // ---- 엣지 ----

    [Fact]
    public void Decode_Empty_ReturnsEmptyText()
    {
        // 빈 입력 판정(21028)은 Validator 몫이다. 해독 단계는 빈 문자열을 돌려준다.
        ImportTextDecoder.Decode([]).Value.Should().BeEmpty();
    }

    [Fact]
    public void Decode_BomOnly_ReturnsEmptyText()
    {
        ImportTextDecoder.Decode([0xEF, 0xBB, 0xBF]).Value.Should().BeEmpty();
    }

    [Fact]
    public void Decode_TwoBoms_StripsOnlyFirst()
    {
        // BOM은 맨 앞 하나만 뗀다. 두 번째는 U+FEFF 문자로 남는다(판정은 Value Object).
        ImportTextDecoder.Decode([0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF]).Value.Should().Be("﻿");
    }

    [Fact]
    public void Decode_BomNotAtStart_IsKeptAsCharacter()
    {
        ImportTextDecoder.Decode([0x41, 0xEF, 0xBB, 0xBF]).Value.Should().Be("A﻿");
    }

    [Fact]
    public void Decode_PartialBom_ReturnsImportInvalidUtf8()
    {
        // EF BB만 있으면 BOM이 아니라 잘린 3바이트 문자다.
        ImportTextDecoder.Decode([0xEF, 0xBB]).Error.Should().BeSameAs(EmployeeErrors.ImportInvalidUtf8);
    }

    [Fact]
    public void Decode_NulByte_IsPreserved()
    {
        // NUL(0x00)은 올바른 UTF-8이다. 거부는 Value Object 몫이라 해독 단계는 값을 보존한다.
        var result = ImportTextDecoder.Decode([0x41, 0x00, 0x42]);

        result.Value.Should().HaveLength(3);
        result.Value[1].Should().Be('\0');
    }
}
