using System.Diagnostics.CodeAnalysis;
using EmergencyHub.BuildingBlocks.Application.Cqrs;
using EmergencyHub.BuildingBlocks.Application.Identifiers;
using EmergencyHub.BuildingBlocks.Domain.Results;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.Extensions.Logging;

namespace EmergencyHub.Employee.Application.Employees.Commands.RegisterEmployee;

/// <summary>
/// 직원 등록 Handler입니다. ID 생성 → Aggregate 등록(정규화) → 정규화한 이메일로 사전 중복 검사 → 추가 순서입니다.
/// </summary>
/// <remarks>
/// <para>저장 · 커밋은 트랜잭션 데코레이터 → <c>IUnitOfWork</c>가 합니다(ADR-0014). Handler는 저장하지 않습니다.</para>
/// <para>
/// 사전 조회는 1차 방어입니다. 동시 요청 경합은 유니크 인덱스 <c>ux_employees_email</c> → 23505 → Infrastructure 매핑이
/// 같은 <see cref="EmployeeErrors.DuplicateEmail"/> 인스턴스로 막습니다(S03-T02).
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "AddConventionalServices가 Scrutor 어셈블리 검색으로 ICommandHandler<,>에 등록해 DI가 만든다(ADR-0015, ADR-0017).")]
internal sealed class RegisterEmployeeCommandHandler(
    IEmployeeRepository repository,
    IIdGenerator idGenerator,
    ILogger<RegisterEmployeeCommandHandler> logger) : ICommandHandler<RegisterEmployeeCommand, EmployeeId>
{
    public async Task<Result<EmployeeId>> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        // 누락(21006)은 검증 데코레이터가 먼저 막는다. 여기까지 null이 오면 예약 값 0으로 넘겨 도메인 불변식이 예외로 막게 한다.
        var employee = Domain.Employees.Employee.Register(
            new EmployeeId(idGenerator.NewId()),
            command.DisplayName,
            command.Email,
            command.EmployeeStatus ?? EmployeeStatus.Unknown);

        if (await repository.ExistsByEmailAsync(employee.Email, cancellationToken))
        {
            return EmployeeErrors.DuplicateEmail;
        }

        repository.Add(employee);
        logger.EmployeeRegistered(employee.Id.Value);

        return employee.Id;
    }
}
