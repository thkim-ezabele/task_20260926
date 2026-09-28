using EmergencyHub.BuildingBlocks.Infrastructure.Persistence.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmployeeAggregate = EmergencyHub.Employee.Domain.Employees.Employee;

namespace EmergencyHub.Employee.Infrastructure.Persistence.Configurations;

/// <summary>
/// <c>employees</c> 매핑입니다(S03 계획 리뷰 Q16 확정안). 쓰기 · 읽기 DbContext가 함께 씁니다.
/// </summary>
/// <remarks>
/// 컬럼 이름(snake_case) · 강타입 ID 변환 · 키 <c>ValueGeneratedNever</c> · 감사 · <c>xmin</c> · 도메인 이벤트 제외는 공통 규칙이 처리하므로
/// 여기서 <c>HasColumnName</c> · <c>HasColumnType</c> · <c>HasConversion</c> · 기본값을 쓰지 않습니다(dba 구현 사양 2).
/// </remarks>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeAggregate>
{
    public void Configure(EntityTypeBuilder<EmployeeAggregate> builder)
    {
        builder.ToTable(EmployeeDbNames.EmployeesTable);

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.DisplayName)
            .HasMaxLength(EmployeeAggregate.DisplayNameMaxLength)
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasMaxLength(EmployeeAggregate.EmailMaxLength)
            .IsRequired();

        builder.HasUniqueIndex(employee => employee.Email, EmployeeDbNames.EmailUniqueIndex);

        builder.Property(employee => employee.EmployeeStatus)
            .HasCodeCheckConstraint();
    }
}
