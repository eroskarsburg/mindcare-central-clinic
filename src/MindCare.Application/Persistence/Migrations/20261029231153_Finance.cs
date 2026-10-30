using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Finance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "commission_percent",
                table: "professionals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                table: "professionals",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_package",
                table: "appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_service_type",
                table: "appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    id_service_type = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_types", x => x.id_service_type);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "session_packages",
                columns: table => new
                {
                    id_package = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_client = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    total_sessions = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    purchased_at = table.Column<DateTime>(type: "date", nullable: false),
                    expires_at = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_packages", x => x.id_package);
                    table.ForeignKey(
                        name: "FK_session_packages_clients_id_client",
                        column: x => x.id_client,
                        principalTable: "clients",
                        principalColumn: "id_client",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payment_entries",
                columns: table => new
                {
                    id_entry = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_payments = table.Column<int>(type: "int", nullable: true),
                    id_package = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    paid_at = table.Column<DateTime>(type: "date", nullable: false),
                    method = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_entries", x => x.id_entry);
                    table.ForeignKey(
                        name: "FK_payment_entries_payments_id_payments",
                        column: x => x.id_payments,
                        principalTable: "payments",
                        principalColumn: "id_payments",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_entries_session_packages_id_package",
                        column: x => x.id_package,
                        principalTable: "session_packages",
                        principalColumn: "id_package",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            // Pagamentos anteriores ao controle por parcelas viram um recebimento único (forma "Outro").
            migrationBuilder.Sql(
                "INSERT INTO payment_entries (id_payments, amount, paid_at, method, note, created_at) " +
                "SELECT p.id_payments, p.paid_price, COALESCE(p.paid_date, DATE(a.scheduled_date)), 'Outro', 'Migrado da versão anterior', NOW() " +
                "FROM payments p INNER JOIN appointments a ON a.id_appointment = p.id_appointment " +
                "WHERE p.paid_price > 0;");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_id_package",
                table: "appointments",
                column: "id_package");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_id_service_type",
                table: "appointments",
                column: "id_service_type");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_id_package",
                table: "payment_entries",
                column: "id_package");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_id_payments",
                table: "payment_entries",
                column: "id_payments");

            migrationBuilder.CreateIndex(
                name: "IX_payment_entries_paid_at",
                table: "payment_entries",
                column: "paid_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_types_name",
                table: "service_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_packages_id_client",
                table: "session_packages",
                column: "id_client");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_service_types_id_service_type",
                table: "appointments",
                column: "id_service_type",
                principalTable: "service_types",
                principalColumn: "id_service_type",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_session_packages_id_package",
                table: "appointments",
                column: "id_package",
                principalTable: "session_packages",
                principalColumn: "id_package",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_service_types_id_service_type",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_session_packages_id_package",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "payment_entries");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropTable(
                name: "session_packages");

            migrationBuilder.DropIndex(
                name: "IX_appointments_id_package",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_id_service_type",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "commission_percent",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "registration_number",
                table: "professionals");

            migrationBuilder.DropColumn(
                name: "id_package",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "id_service_type",
                table: "appointments");
        }
    }
}
