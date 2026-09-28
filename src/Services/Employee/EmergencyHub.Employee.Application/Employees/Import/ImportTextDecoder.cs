using System.Buffers;
using System.Text.Unicode;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 일괄 가져오기 입력의 엄격 UTF-8 해독입니다. CSV · JSON 파서 앞 공통 단계입니다(ADR-0026 3절, PRD-002 FR-03).
/// </summary>
/// <remarks>
/// 맨 앞 BOM(<c>EF BB BF</c>) 하나만 떼고, 잘못된 바이트(CP949, UTF-8로 인코딩한 서로게이트 <c>ED A0 80</c>, 과잉 길이, 잘린 문자 등)는
/// 치환하지 않고 요청 전체 오류 21022로 돌려줍니다. NUL 등 제어 문자는 올바른 UTF-8이므로 보존합니다(거부는 Value Object).
/// </remarks>
internal static class ImportTextDecoder
{
    private static ReadOnlySpan<byte> Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>바이트를 UTF-8로 해독합니다.</summary>
    /// <param name="content">입력 바이트.</param>
    /// <returns>성공이면 BOM을 뗀 문자열, 실패면 <see cref="EmployeeErrors.ImportInvalidUtf8"/>.</returns>
    public static Result<string> Decode(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(Bom))
        {
            content = content[Bom.Length..];
        }

        // UTF-16 코드 단위 수는 UTF-8 바이트 수를 넘지 않는다.
        var buffer = new char[content.Length];
        var status = Utf8.ToUtf16(content, buffer, out _, out var charsWritten, replaceInvalidSequences: false);

        return status == OperationStatus.Done
            ? new string(buffer, 0, charsWritten)
            : EmployeeErrors.ImportInvalidUtf8;
    }
}
