using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarbershopReservationsUni.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOneTimeCodesAndEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_BarberId",
                table: "Appointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Вече съществуващи резервации: краят = начало + продължителност на услугата
            migrationBuilder.Sql(
                @"UPDATE a SET a.EndDate = DATEADD(MINUTE, s.DurationMinutes, a.AppointmentDate)
                  FROM Appointments a INNER JOIN Services s ON s.Id = a.ServiceId");

            migrationBuilder.CreateTable(
                name: "OneTimeCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BarberId_AppointmentDate_EndDate",
                table: "Appointments",
                columns: new[] { "BarberId", "AppointmentDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OneTimeCodes_PhoneNumber_CreatedOn",
                table: "OneTimeCodes",
                columns: new[] { "PhoneNumber", "CreatedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeCodes");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BarberId_AppointmentDate_EndDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BarberId",
                table: "Appointments",
                column: "BarberId");
        }
    }
}
