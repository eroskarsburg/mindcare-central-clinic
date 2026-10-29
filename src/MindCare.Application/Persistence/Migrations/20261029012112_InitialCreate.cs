using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id_client = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    gender = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    age = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.id_client);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id_user = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    password = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    id_access_level = table.Column<int>(type: "int", nullable: false),
                    last_activity = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id_user);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id_appointment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_client = table.Column<int>(type: "int", nullable: false),
                    modality = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    scheduled_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    observation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id_appointment);
                    table.ForeignKey(
                        name: "FK_appointments_clients_id_client",
                        column: x => x.id_client,
                        principalTable: "clients",
                        principalColumn: "id_client",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "professionals",
                columns: table => new
                {
                    id_professional = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_user_prof = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    gender = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    speciality = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professionals", x => x.id_professional);
                    table.ForeignKey(
                        name: "FK_professionals_users_id_user_prof",
                        column: x => x.id_user_prof,
                        principalTable: "users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id_payments = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_appointment = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    paid_price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    paid_date = table.Column<DateTime>(type: "date", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id_payments);
                    table.ForeignKey(
                        name: "FK_payments_appointments_id_appointment",
                        column: x => x.id_appointment,
                        principalTable: "appointments",
                        principalColumn: "id_appointment",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_id_client",
                table: "appointments",
                column: "id_client");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_scheduled_date",
                table: "appointments",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "IX_clients_cpf",
                table: "clients",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_id_appointment",
                table: "payments",
                column: "id_appointment",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_professionals_cpf",
                table: "professionals",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_professionals_id_user_prof",
                table: "professionals",
                column: "id_user_prof",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "professionals");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
