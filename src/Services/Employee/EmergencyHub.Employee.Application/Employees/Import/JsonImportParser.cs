using System.Collections.Frozen;
using System.Text.Json;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 일괄 가져오기 JSON 파서입니다. <see cref="JsonDocument"/>로 읽는 순수 클래스입니다(PRD-002 FR-04, ADR-0026 3 · 5 · 6절, 패키지 없음).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>받는 형태: 배열 <c>[{...}]</c>, 단일 객체 <c>{...}</c>, 대괄호 없는 나열 <c>{...},{...}</c>. 첫 공백 아닌 문자가 <c>{</c>이면 <c>[</c> · <c>]</c>로 감싼 뒤 읽는다.
/// 그 밖의 루트(숫자 · 문자열 · <c>null</c> · <c>true</c>)는 받는 형태가 아니라 문법 오류 21023이다.</item>
/// <item><see cref="JsonDocumentOptions"/> 기본값(끝 쉼표 · 주석 불허, 최대 깊이 64)을 쓴다. 감쌀 때는 감싼 대괄호 한 단계만 더해 <b>입력 기준</b> 최대 깊이를 64로 맞춘다.</item>
/// <item>속성 이름은 대소문자 무시(<see cref="StringComparer.OrdinalIgnoreCase"/>), 알 수 없는 속성은 무시한다(중복이어도). 속성이 없으면 필드는 <see langword="null"/>이고 필수 판정은 Value Object 몫이다.
/// 문자열 값은 원문 그대로(앞뒤 공백 · 제어 문자 보존) 넘긴다.</item>
/// <item>항목 오류(항목마다 속성 순서로 첫 오류 하나): 객체가 아님 21024, 읽는 필드가 두 번 이상(대소문자만 다른 이름 포함) 21026, 읽는 필드 값이 문자열이 아님(<c>null</c> 포함) 21025.</item>
/// <item>요청 전체 오류: 잘못된 UTF-8 21022(<see cref="ImportTextDecoder"/>), 문법 오류 21023, 항목 수(오류 항목 포함)가 <see cref="ImportLimits.MaxRows"/>를 넘으면 21027,
/// 속성 이름 또는 읽는 필드 값의 짝 없는 서로게이트 이스케이프(<c>\ud800</c> 등) 21022. 알 수 없는 속성의 값은 읽지 않으므로 판정하지 않는다.</item>
/// </list>
/// 오류에는 고정 문구만 담고 <see cref="JsonException"/> · <see cref="InvalidOperationException"/> 원문과 입력 값을 넣지 않는다(FR-04, NFR-04).
/// </remarks>
internal static class JsonImportParser
{
    private const int MaxInputDepth = 64;

    private static readonly FrozenDictionary<string, ImportField> Fields = new Dictionary<string, ImportField>(StringComparer.OrdinalIgnoreCase)
    {
        ["name"] = ImportField.Name,
        ["email"] = ImportField.Email,
        ["tel"] = ImportField.Tel,
        ["joined"] = ImportField.Joined,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>JSON 바이트를 행 목록과 항목 오류 목록으로 읽습니다.</summary>
    /// <param name="content">입력 바이트(UTF-8, BOM 허용).</param>
    /// <returns>성공이면 <see cref="ImportParseResult"/>, 요청 전체 오류면 21022 · 21023 · 21027.</returns>
    public static Result<ImportParseResult> Parse(ReadOnlySpan<byte> content)
    {
        var decoded = ImportTextDecoder.Decode(content);
        if (decoded.IsFailure)
        {
            return Result.Failure<ImportParseResult>(decoded.Error);
        }

        var text = decoded.Value;
        var start = FirstNonWhitespaceIndex(text);
        if (start < 0)
        {
            // 빈 입력 판정(21028)은 Validator와 Handler(행 0개) 몫이다. CSV 파서와 같이 빈 결과를 돌려준다.
            return new ImportParseResult([], []);
        }

        var isUnbracketed = text[start] == '{';
        var json = isUnbracketed ? $"[{text}]" : text;
        var options = new JsonDocumentOptions { MaxDepth = isUnbracketed ? MaxInputDepth + 1 : MaxInputDepth };

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, options);
        }
        catch (JsonException)
        {
            // 예외 원문(줄 · 위치 · 입력 조각)은 버리고 고정 코드만 돌려준다(FR-04).
            return EmployeeErrors.JsonSyntaxInvalid;
        }

        using (document)
        {
            return ReadItems(document.RootElement);
        }
    }

    private static int FirstNonWhitespaceIndex(string text)
    {
        // JSON 공백(RFC 8259): 공백 · 탭 · LF · CR.
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is not (' ' or '\t' or '\n' or '\r'))
            {
                return index;
            }
        }

        return -1;
    }

    private static Result<ImportParseResult> ReadItems(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return EmployeeErrors.JsonSyntaxInvalid;
        }

        if (root.GetArrayLength() > ImportLimits.MaxRows)
        {
            return EmployeeErrors.ImportTooManyRows;
        }

        var rows = new List<ImportRow>();
        var errors = new List<ImportRowError>();
        var number = 0;
        foreach (var item in root.EnumerateArray())
        {
            number++;
            if (item.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new ImportRowError(number, EmployeeErrors.JsonItemNotObject));
                continue;
            }

            var itemResult = ReadItem(number, item);
            if (itemResult.IsFailure)
            {
                return Result.Failure<ImportParseResult>(itemResult.Error);
            }

            switch (itemResult.Value)
            {
                case { Row: { } row }:
                    rows.Add(row);
                    break;
                case { Error: { } error }:
                    errors.Add(error);
                    break;
            }
        }

        return new ImportParseResult(rows.AsReadOnly(), errors.AsReadOnly());
    }

    /// <returns>행 또는 항목 오류. 요청 전체 오류(짝 없는 서로게이트 21022)면 실패.</returns>
    private static Result<ItemOutcome> ReadItem(int number, JsonElement item)
    {
        // 인덱스는 ImportField 값(1 ~ 4). 0(None)은 쓰지 않는다.
        var values = new string?[5];
        var seen = new bool[5];
        ImportRowError? itemError = null;

        // 항목 오류가 나도 끝까지 읽어 속성 이름 · 읽는 필드 값의 21022를 놓치지 않는다.
        foreach (var property in item.EnumerateObject())
        {
            if (!TryReadName(property, out var name))
            {
                return EmployeeErrors.ImportInvalidUtf8;
            }

            if (!Fields.TryGetValue(name, out var field))
            {
                continue;
            }

            var index = (int)field;
            if (seen[index])
            {
                itemError ??= new ImportRowError(number, EmployeeErrors.JsonDuplicateProperty, field);
                continue;
            }

            seen[index] = true;
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                itemError ??= new ImportRowError(number, EmployeeErrors.JsonValueNotString, field);
                continue;
            }

            if (!TryReadString(property.Value, out values[index]))
            {
                return EmployeeErrors.ImportInvalidUtf8;
            }
        }

        return itemError is null
            ? new ItemOutcome(new ImportRow(number, values[1], values[2], values[3], values[4]), null)
            : new ItemOutcome(null, itemError);
    }

    private static bool TryReadName(JsonProperty property, out string name)
    {
        try
        {
            name = property.Name;
            return true;
        }
        catch (InvalidOperationException)
        {
            // JSON 이스케이프의 짝 없는 서로게이트는 UTF-16 문자열로 바꿀 수 없다(.NET 8 JsonDocument는 파싱 때 검사하지 않음). 원문은 버린다.
            name = string.Empty;
            return false;
        }
    }

    private static bool TryReadString(JsonElement value, out string? text)
    {
        try
        {
            text = value.GetString();
            return true;
        }
        catch (InvalidOperationException)
        {
            // ValueKind가 String임을 확인한 뒤라 이 예외는 짝 없는 서로게이트 이스케이프뿐이다. 원문은 버린다.
            text = null;
            return false;
        }
    }

    /// <summary>항목 하나를 읽은 결과입니다. 둘 중 하나만 값이 있습니다.</summary>
    /// <param name="Row">읽은 행.</param>
    /// <param name="Error">항목 오류.</param>
    private sealed record ItemOutcome(ImportRow? Row, ImportRowError? Error);
}
