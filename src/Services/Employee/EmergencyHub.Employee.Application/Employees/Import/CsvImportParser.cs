using System.Text;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;

namespace EmergencyHub.Employee.Application.Employees.Import;

/// <summary>
/// 일괄 가져오기 CSV 파서입니다. 상태 기계로 직접 구현한 순수 클래스입니다(PRD-002 FR-03, ADR-0026 3 · 4 · 6절, 패키지 없음).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>헤더 없음(첫 줄도 데이터, TD-028), 열 순서 고정 <c>name,email,tel,joined</c>, 쉼표 구분.</item>
/// <item>RFC 4180 따옴표: 따옴표 안의 쉼표 · 줄바꿈은 필드 문자이고 <c>""</c>는 <c>"</c> 하나. 여는 따옴표 앞 · 닫는 따옴표 뒤의 공백은 허용하고 버린다.</item>
/// <item>줄 끝은 <c>\n</c> · <c>\r\n</c>뿐이다. CR 단독은 필드 안 문자이고, 필드 앞뒤에 있으면 trim으로 지워진다.</item>
/// <item>모든 필드(따옴표로 감싼 필드 포함)는 <see cref="string.Trim()"/>으로 앞뒤 공백을 제거한다.</item>
/// <item>빈 줄(따옴표 없이 비었거나 공백만 있는 줄)은 무시하되 줄 번호에는 센다. 행 번호는 레코드가 시작하는 물리 줄 번호(1부터)다.</item>
/// <item>행 오류(행마다 첫 오류 하나): 열 개수가 4가 아님 21019, 닫히지 않은 따옴표 21020, 따옴표 없는 필드 안 · 닫는 따옴표 뒤의 <c>"</c> · 문자 21021.
/// 21021이 나면 그 물리 줄 끝까지 건너뛰고 다음 줄부터 다시 읽는다.</item>
/// <item>요청 전체 오류: 잘못된 UTF-8 21022(<see cref="ImportTextDecoder"/>), 행 수(빈 줄 제외, 행 오류 행 포함)가 <see cref="ImportLimits.MaxRows"/>를 넘으면 21027로 멈춘다.</item>
/// </list>
/// 오류에는 고정 문구만 담고 입력 값을 넣지 않는다(NFR-04). 빈 입력 판정(21028)은 Validator 몫이다.
/// </remarks>
internal static class CsvImportParser
{
    private const int ColumnCount = 4;

    /// <summary>CSV 바이트를 행 목록과 행 오류 목록으로 읽습니다.</summary>
    /// <param name="content">입력 바이트(UTF-8, BOM 허용).</param>
    /// <returns>성공이면 <see cref="ImportParseResult"/>, 요청 전체 오류면 21022 또는 21027.</returns>
    public static Result<ImportParseResult> Parse(ReadOnlySpan<byte> content)
    {
        var decoded = ImportTextDecoder.Decode(content);
        if (decoded.IsFailure)
        {
            return Result.Failure<ImportParseResult>(decoded.Error);
        }

        return new Machine(decoded.Value).Run();
    }

    private enum State : short
    {
        /// <summary>쓰지 않음(0 예약, 코드값 규칙).</summary>
        None = 0,

        /// <summary>필드 시작. 아직 공백만 읽었다.</summary>
        FieldStart = 1,

        /// <summary>따옴표 없는 필드 안.</summary>
        Unquoted = 2,

        /// <summary>따옴표로 감싼 필드 안.</summary>
        Quoted = 3,

        /// <summary>따옴표로 감싼 필드 안에서 <c>"</c>를 읽었다(닫는 따옴표 또는 <c>""</c>의 앞).</summary>
        QuoteInQuoted = 4,

        /// <summary>닫는 따옴표 뒤. 공백만 허용한다.</summary>
        AfterClosingQuote = 5,

        /// <summary>행 오류 뒤. 물리 줄 끝까지 건너뛴다.</summary>
        SkipToLineEnd = 6,
    }

    private sealed class Machine(string text)
    {
        private readonly List<ImportRow> _rows = [];
        private readonly List<ImportRowError> _errors = [];
        private readonly List<string> _fields = new(ColumnCount);
        private readonly StringBuilder _field = new();

        private State _state = State.FieldStart;
        private int _line = 1;
        private int _recordLine = 1;
        private bool _recordHasQuote;
        private Error? _recordError;
        private int _recordCount;

        public Result<ImportParseResult> Run()
        {
            var index = 0;
            while (index < text.Length)
            {
                var current = text[index];
                var lineBreakLength = LineBreakLength(index);

                if (lineBreakLength > 0 && _state == State.Quoted)
                {
                    // 따옴표 안 줄바꿈은 필드 문자다. 물리 줄 번호만 늘린다.
                    _field.Append(text, index, lineBreakLength);
                    _line++;
                }
                else if (lineBreakLength > 0)
                {
                    if (!EndRecord())
                    {
                        return EmployeeErrors.ImportTooManyRows;
                    }

                    _line++;
                    _recordLine = _line;
                }
                else
                {
                    Read(current);
                }

                index += Math.Max(lineBreakLength, 1);
            }

            if (_state == State.Quoted)
            {
                _recordError = EmployeeErrors.CsvUnclosedQuote;
            }

            if (!EndRecord())
            {
                return EmployeeErrors.ImportTooManyRows;
            }

            return new ImportParseResult(_rows.AsReadOnly(), _errors.AsReadOnly());
        }

        private int LineBreakLength(int index) => text[index] switch
        {
            '\n' => 1,
            '\r' when index + 1 < text.Length && text[index + 1] == '\n' => 2,
            _ => 0,
        };

        private void Read(char current)
        {
            switch (_state)
            {
                case State.FieldStart when current == '"':
                    // 여는 따옴표 앞의 공백은 버린다(trim 규칙).
                    _field.Clear();
                    _recordHasQuote = true;
                    _state = State.Quoted;
                    break;
                case State.FieldStart or State.Unquoted when current == ',':
                    EndField();
                    break;
                case State.FieldStart:
                    _field.Append(current);
                    _state = char.IsWhiteSpace(current) ? State.FieldStart : State.Unquoted;
                    break;
                case State.Unquoted when current == '"':
                    Fail(EmployeeErrors.CsvUnexpectedQuote);
                    break;
                case State.Unquoted:
                    _field.Append(current);
                    break;
                case State.Quoted when current == '"':
                    _state = State.QuoteInQuoted;
                    break;
                case State.Quoted:
                    _field.Append(current);
                    break;
                case State.QuoteInQuoted when current == '"':
                    _field.Append('"');
                    _state = State.Quoted;
                    break;
                case State.QuoteInQuoted or State.AfterClosingQuote when current == ',':
                    EndField();
                    break;
                case State.QuoteInQuoted or State.AfterClosingQuote when char.IsWhiteSpace(current):
                    _state = State.AfterClosingQuote;
                    break;
                case State.QuoteInQuoted or State.AfterClosingQuote:
                    Fail(EmployeeErrors.CsvUnexpectedQuote);
                    break;
                default:
                    // SkipToLineEnd: 줄 끝까지 따옴표를 포함해 모두 버린다.
                    break;
            }
        }

        private void Fail(Error error)
        {
            _recordError = error;
            _state = State.SkipToLineEnd;
        }

        private void EndField()
        {
            _fields.Add(_field.ToString().Trim());
            _field.Clear();
            _state = State.FieldStart;
        }

        /// <returns>행 수 상한 안이면 <see langword="true"/>.</returns>
        private bool EndRecord()
        {
            if (_recordError is null)
            {
                EndField();
            }

            var isBlankLine = _recordError is null && !_recordHasQuote && _fields is [{ Length: 0 }];
            if (!isBlankLine)
            {
                _recordCount++;
                if (_recordCount > ImportLimits.MaxRows)
                {
                    return false;
                }

                AddRecord();
            }

            _fields.Clear();
            _field.Clear();
            _recordError = null;
            _recordHasQuote = false;
            _state = State.FieldStart;
            return true;
        }

        private void AddRecord()
        {
            if (_recordError is not null)
            {
                _errors.Add(new ImportRowError(_recordLine, _recordError));
            }
            else if (_fields.Count != ColumnCount)
            {
                _errors.Add(new ImportRowError(_recordLine, EmployeeErrors.CsvColumnCountMismatch));
            }
            else
            {
                _rows.Add(new ImportRow(_recordLine, _fields[0], _fields[1], _fields[2], _fields[3]));
            }
        }
    }
}
