using System.Globalization;
using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using EmergencyHub.Employee.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>employees</c> 매핑입니다(database.md Employee "새 스키마 명세", PRD-002 FR-01 · FR-02). 쓰기 · 읽기 DbContext가 함께 씁니다.
/// </summary>
/// <remarks>
/// <para>
/// 컬럼 이름(snake_case) · 강타입 ID 변환 · 키 <c>ValueGeneratedNever</c> · 감사 · <c>xmin</c> · 도메인 이벤트 제외는 공통 규칙이 처리합니다.
/// 여기서 <c>HasColumnName</c> · <c>HasColumnType</c> · <c>HasColumnOrder</c> · 기본값(<c>HasDefaultValue</c> · <c>HasDefaultValueSql</c>)을 쓰지 않습니다.
/// 열 순서는 Aggregate 공개 속성의 선언 순서입니다.
/// </para>
/// <para>
/// Value Object 4개는 이 설정 안의 값 변환기로 스칼라 컬럼에 매핑합니다(공통 규약으로 넓히지 않음, Owned · Complex Type 미사용).
/// DB → 모델 변환은 VO의 <c>Create(...).Value</c>를 거치므로, DB 값이 VO 규칙을 어기면 구체화 때 예외가 납니다.
/// <see cref="EmployeeAggregate.NormalizedEmail"/>은 변환기 없는 <see cref="string"/>이고 유니크 인덱스는 이 컬럼에만 겁니다(<c>email</c> 인덱스 없음).
/// </para>
/// <para>
/// <c>ix_</c> 인덱스 2개는 이름을 명명 규칙(EFCore.NamingConventions)이 만들도록 <c>HasDatabaseName</c>을 쓰지 않습니다.
/// </para>
/// </remarks>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeAggregate>
{
    public void Configure(EntityTypeBuilder<EmployeeAggregate> builder)
    {
        builder.ToTable(EmployeeDbNames.EmployeesTable);

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.Name)
            .HasConversion(name => name.Value, value => Name.Create(value).Value)
            .HasMaxLength(Name.MaxLength)
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(employee => employee.NormalizedEmail)
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(employee => employee.PhoneNumber)
            .HasConversion(phoneNumber => phoneNumber.Value, value => PhoneNumber.Create(value).Value)
            .HasMaxLength(PhoneNumber.MaxLength)
            .IsRequired();

        builder.Property(employee => employee.JoinedOn)
            .HasConversion(
                joinedOn => joinedOn.Value,
                value => JoinedOn.Create(value.ToString(JoinedOn.Format, CultureInfo.InvariantCulture)).Value)
            .IsRequired();

        builder.Property(employee => employee.EmployeeStatus)
            .HasCodeCheckConstraint();

        builder.HasUniqueIndex(employee => employee.NormalizedEmail, EmployeeDbNames.NormalizedEmailUniqueIndex);

        builder.HasIndex(employee => new { employee.JoinedOn, employee.Id });

        builder.HasIndex(employee => new { employee.Name, employee.JoinedOn, employee.Id });
    }
}
