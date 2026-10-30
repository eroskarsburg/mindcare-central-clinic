using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndLgpd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_until",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "anonymized_at",
                table: "clients",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "consent_at",
                table: "clients",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id_audit = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    id_user = table.Column<int>(type: "int", nullable: true),
                    username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    action = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    entity = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    details = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id_audit);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_entity_id",
                table: "audit_logs",
                columns: new[] { "entity", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                table: "users");

            migrationBuilder.DropColumn(
                name: "anonymized_at",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "consent_at",
                table: "clients");
        }
    }
}
