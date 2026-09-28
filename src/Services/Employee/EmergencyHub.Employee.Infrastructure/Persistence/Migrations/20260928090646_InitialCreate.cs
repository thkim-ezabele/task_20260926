using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyHub.Employee.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_on = table.Column<DateOnly>(type: "date", nullable: false),
                    employee_status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.CheckConstraint("ck_employees_employee_status", "employee_status IN (1, 2)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_joined_on_id",
                table: "employees",
                columns: new[] { "joined_on", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_name_joined_on_id",
                table: "employees",
                columns: new[] { "name", "joined_on", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_employees_normalized_email",
                table: "employees",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
