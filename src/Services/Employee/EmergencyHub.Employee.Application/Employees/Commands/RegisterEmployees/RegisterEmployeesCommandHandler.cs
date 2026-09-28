using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Domain.Errors;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Application.Employees.Import;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployees;

/// <summary>
/// 일괄 등록 Handler입니다(PRD-002 FR-06, ADR-0026 6 · 7 · 9절). 앞 단계가 실패하면 멈추고 아무것도 추가하지 않습니다.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>파싱: <see cref="RegisterEmployeesCommand.Format"/>에 맞는 파서. 요청 전체 오류(21022 · 21023 · 21027)는 경로 ""의 <see cref="FieldError"/> 하나로 옮긴 <see cref="ValidationError"/>입니다.</item>
/// <item>행 검증: 파서 행 오류(경로 <c>Rows[n]</c> 또는 <c>Rows[n].필드</c>)와 행마다 Value Object <c>Create</c> 결과(필드 순서 Name → Email → Tel → Joined,
/// 필드마다 첫 오류)를 행 번호 순으로 한 목록에 모읍니다.</item>
/// <item>요청 안 이메일 중복: <see cref="Email.NormalizedEmail"/> 서수 비교, 같은 값의 행을 모두 <c>Rows[n].Email</c> · 21018로 표시합니다.</item>
/// <item>DB 사전 조회: 정규화 이메일 목록 한 번(<see cref="IEmployeeRepository.ListExistingNormalizedEmailsAsync"/>, 쓰기 연결). 결과 순서를 가정하지 않고
/// NormalizedEmail → 행 번호 사전으로 짝지어 입력 순서로 <see cref="ConflictError"/>(23001, <c>Rows[n].Email</c>)를 만듭니다.</item>
/// <item>Aggregate 생성 · <see cref="IEmployeeRepository.AddRange"/>. ID는 행 순서대로 <see cref="IIdGenerator"/>로 만듭니다.</item>
/// </list>
/// <para>
/// ①~③의 실패 목록(400)과 ④의 충돌 목록(409)은 각각 앞에서 <see cref="MaxReportedRows"/>개까지 담고, 넘으면 경로 ""의 잘림 항목
/// (21030 · 23002) 하나를 붙입니다. <see cref="ValidationError"/>를 Handler가 만드는 것은 이 Handler에만 허용한 ADR-0018 범위 예외입니다(ADR-0026 7절).
/// </para>
/// <para>
/// 행이 0개(예: JSON <c>[]</c>)이면 조회 · 추가 없이 0건 성공입니다. 저장 · 커밋은 트랜잭션 데코레이터 → <c>IUnitOfWork</c>가 하고(ADR-0014),
/// 동시 경합은 <c>ux_employees_normalized_email</c> 23505 → 23001(행 번호 없음)로 막습니다(ADR-0026 9절). 로그에는 직원 ID만 남깁니다(NFR-04).
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 Scrutor 어셈블리 검색으로 ICommandHandler<,>에 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class RegisterEmployeesCommandHandler(
    IEmployeeRepository repository,
    IIdGenerator idGenerator,
    ILogger<RegisterEmployeesCommandHandler> logger) : ICommandHandler<RegisterEmployeesCommand, RegisterEmployeesResponse>
{
    /// <summary>400 · 409 목록에 담는 최대 행 항목 수(ADR-0026 6절). 넘으면 잘림 항목 하나를 더 붙입니다.</summary>
    internal const int MaxReportedRows = 100;

    public async Task<Result<RegisterEmployeesResponse>> Handle(RegisterEmployeesCommand command, CancellationToken cancellationToken)
    {
        var validated = ParseAndValidate(command);
        if (validated.IsFailure)
        {
            return Result.Failure<RegisterEmployeesResponse>(validated.Error);
        }

        var rows = validated.Value;
        if (rows.Count == 0)
        {
            return new RegisterEmployeesResponse(0, []);
        }

        var existing = await repository.ListExistingNormalizedEmailsAsync(
            [.. rows.Select(row => row.Email.NormalizedEmail)], cancellationToken);
        var conflictRows = FindStoredEmailRows(rows, existing);
        if (conflictRows.Count > 0)
        {
            return ToConflictError(conflictRows);
        }

        List<Domain.Employees.Employee> employees =
        [
            .. rows.Select(row => Domain.Employees.Employee.Register(
                new EmployeeId(idGenerator.NewId()), row.Name, row.Email, row.PhoneNumber, row.JoinedOn)),
        ];
        repository.AddRange(employees);

        foreach (var employee in employees)
        {
            logger.EmployeeRegistered(employee.Id.Value);
        }

        return new RegisterEmployeesResponse(employees.Count, [.. employees.Select(employee => employee.Id.Value)]);
    }

    // ①~③: 동기 단계. 입력 바이트(Span)를 await 너머로 들고 가지 않도록 나눈다.
    private static Result<IReadOnlyList<ValidRow>> ParseAndValidate(RegisterEmployeesCommand command)
    {
        var parsed = Parse(command.Format, command.Content.Span);
        if (parsed.IsFailure)
        {
            // 파서의 요청 전체 오류(21022 · 21023 · 21027)는 경로 "" 하나로 옮긴다.
            return ValidationError.Create([FieldError.Create(string.Empty, parsed.Error)]);
        }

        var rowErrors = new List<(int RowNumber, FieldError Error)>();
        foreach (var error in parsed.Value.Errors)
        {
            rowErrors.Add((error.RowNumber, FieldError.Create(RowPath(error.RowNumber, error.Field), error.Error)));
        }

        var rows = new List<ValidRow>(parsed.Value.Rows.Count);
        foreach (var row in parsed.Value.Rows)
        {
            var name = Name.Create(row.Name);
            var email = Email.Create(row.Email);
            var phoneNumber = PhoneNumber.Create(row.Tel);
            var joinedOn = JoinedOn.Create(row.Joined);

            AddIfFailed(rowErrors, row.RowNumber, ImportField.Name, name);
            AddIfFailed(rowErrors, row.RowNumber, ImportField.Email, email);
            AddIfFailed(rowErrors, row.RowNumber, ImportField.Tel, phoneNumber);
            AddIfFailed(rowErrors, row.RowNumber, ImportField.Joined, joinedOn);

            if (name.IsSuccess && email.IsSuccess && phoneNumber.IsSuccess && joinedOn.IsSuccess)
            {
                rows.Add(new ValidRow(row.RowNumber, name.Value, email.Value, phoneNumber.Value, joinedOn.Value));
            }
        }

        if (rowErrors.Count > 0)
        {
            // 파서 행 오류와 필드 오류는 서로 다른 행이다. 안정 정렬이라 한 행 안의 필드 순서는 유지된다.
            return ToValidationError(rowErrors.OrderBy(entry => entry.RowNumber).Select(entry => entry.Error));
        }

        var duplicates = FindDuplicateEmailRows(rows);
        if (duplicates.Count > 0)
        {
            return ToValidationError(duplicates.Select(
                rowNumber => FieldError.Create(RowPath(rowNumber, ImportField.Email), EmployeeErrors.DuplicateEmailInRequest)));
        }

        return Result.Success<IReadOnlyList<ValidRow>>(rows);
    }

    private static Result<ImportParseResult> Parse(EmployeeImportFormat format, ReadOnlySpan<byte> content) => format switch
    {
        EmployeeImportFormat.Csv => CsvImportParser.Parse(content),
        EmployeeImportFormat.Json => JsonImportParser.Parse(content),
        _ => throw new InvalidOperationException("정의되지 않은 입력 형식입니다. Validator가 먼저 1002로 거부해야 합니다."),
    };

    private static void AddIfFailed(List<(int RowNumber, FieldError Error)> rowErrors, int rowNumber, ImportField field, Result result)
    {
        if (result.IsFailure)
        {
            rowErrors.Add((rowNumber, FieldError.Create(RowPath(rowNumber, field), result.Error)));
        }
    }

    private static List<int> FindDuplicateEmailRows(List<ValidRow> rows) =>
        [
            .. rows
                .GroupBy(row => row.Email.NormalizedEmail, StringComparer.Ordinal)
                .Where(group => group.Skip(1).Any())
                .SelectMany(group => group.Select(row => row.RowNumber))
                .Order(),
        ];

    private static List<int> FindStoredEmailRows(IReadOnlyList<ValidRow> rows, List<string> storedNormalizedEmails)
    {
        // ③을 지났으므로 NormalizedEmail은 행마다 다르다. 넘기지 않은 값 · 반복된 값은 짝지을 행이 없거나 한 번만 센다.
        var rowNumberByEmail = rows.ToDictionary(row => row.Email.NormalizedEmail, row => row.RowNumber, StringComparer.Ordinal);
        var conflictRows = new SortedSet<int>();
        foreach (var stored in storedNormalizedEmails)
        {
            if (rowNumberByEmail.TryGetValue(stored, out var rowNumber))
            {
                conflictRows.Add(rowNumber);
            }
        }

        return [.. conflictRows];
    }

    private static ValidationError ToValidationError(IEnumerable<FieldError> errors)
    {
        var reported = errors.Take(MaxReportedRows + 1).ToList();
        if (reported.Count > MaxReportedRows)
        {
            reported[MaxReportedRows] = FieldError.Create(string.Empty, EmployeeErrors.RowErrorsTruncated);
        }

        return ValidationError.Create(reported);
    }

    private static ConflictError ToConflictError(List<int> conflictRows)
    {
        var details = conflictRows
            .Take(MaxReportedRows)
            .Select(rowNumber => ConflictDetail.Create(RowPath(rowNumber, ImportField.Email), EmployeeErrors.DuplicateEmail))
            .ToList();
        if (conflictRows.Count > MaxReportedRows)
        {
            details.Add(ConflictDetail.Create(string.Empty, EmployeeErrors.RowConflictsTruncated));
        }

        return ConflictError.Create(EmployeeErrors.DuplicateEmail, details);
    }

    private static string RowPath(int rowNumber, ImportField field) => field switch
    {
        ImportField.None => string.Create(CultureInfo.InvariantCulture, $"Rows[{rowNumber}]"),
        ImportField.Name => string.Create(CultureInfo.InvariantCulture, $"Rows[{rowNumber}].Name"),
        ImportField.Email => string.Create(CultureInfo.InvariantCulture, $"Rows[{rowNumber}].Email"),
        ImportField.Tel => string.Create(CultureInfo.InvariantCulture, $"Rows[{rowNumber}].Tel"),
        ImportField.Joined => string.Create(CultureInfo.InvariantCulture, $"Rows[{rowNumber}].Joined"),
        _ => throw new InvalidOperationException("정의되지 않은 가져오기 필드입니다."),
    };

    /// <summary>행 검증을 통과한 행입니다(Value Object 4개).</summary>
    private sealed record ValidRow(int RowNumber, Name Name, Email Email, PhoneNumber PhoneNumber, JoinedOn JoinedOn);
}
