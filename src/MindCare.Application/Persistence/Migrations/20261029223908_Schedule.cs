using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Schedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "appointments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_minutes",
                table: "appointments",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "id_professional",
                table: "appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meeting_url",
                table: "appointments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "series_id",
                table: "appointments",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "appointments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Agendada");

            // Consultas anteriores a esta versão: as que já passaram são consideradas realizadas.
            migrationBuilder.Sql("UPDATE appointments SET status = 'Realizada' WHERE scheduled_date < NOW();");

            migrationBuilder.CreateTable(
                name: "professional_availability",
                columns: table => new
                {
                    id_availability = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_professional = table.Column<int>(type: "int", nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professional_availability", x => x.id_availability);
                    table.ForeignKey(
                        name: "FK_professional_availability_professionals_id_professional",
                        column: x => x.id_professional,
                        principalTable: "professionals",
                        principalColumn: "id_professional",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "schedule_blocks",
                columns: table => new
                {
                    id_block = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_professional = table.Column<int>(type: "int", nullable: true),
                    starts_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ends_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reason = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_blocks", x => x.id_block);
                    table.ForeignKey(
                        name: "FK_schedule_blocks_professionals_id_professional",
                        column: x => x.id_professional,
                        principalTable: "professionals",
                        principalColumn: "id_professional",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_id_professional_scheduled_date",
                table: "appointments",
                columns: new[] { "id_professional", "scheduled_date" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_series_id",
                table: "appointments",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_professional_availability_id_professional_day_of_week",
                table: "professional_availability",
                columns: new[] { "id_professional", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_blocks_id_professional",
                table: "schedule_blocks",
                column: "id_professional");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_blocks_starts_at_ends_at",
                table: "schedule_blocks",
                columns: new[] { "starts_at", "ends_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_professionals_id_professional",
                table: "appointments",
                column: "id_professional",
                principalTable: "professionals",
                principalColumn: "id_professional",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_professionals_id_professional",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "professional_availability");

            migrationBuilder.DropTable(
                name: "schedule_blocks");

            migrationBuilder.DropIndex(
                name: "IX_appointments_id_professional_scheduled_date",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_series_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "duration_minutes",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "id_professional",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "meeting_url",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "series_id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "appointments");
        }
    }
}
