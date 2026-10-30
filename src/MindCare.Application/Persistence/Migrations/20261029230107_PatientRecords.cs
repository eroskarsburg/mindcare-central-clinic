using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PatientRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "clients",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "birth_date",
                table: "clients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "clients",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "clients",
                type: "varchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "clients",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_responsible_professional",
                table: "clients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "clients",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "clients",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ativo");

            migrationBuilder.CreateTable(
                name: "client_documents",
                columns: table => new
                {
                    id_document = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_client = table.Column<int>(type: "int", nullable: false),
                    category = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    storage_path = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    content_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    id_user_uploaded = table.Column<int>(type: "int", nullable: false),
                    id_professional_uploaded = table.Column<int>(type: "int", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_documents", x => x.id_document);
                    table.ForeignKey(
                        name: "FK_client_documents_clients_id_client",
                        column: x => x.id_client,
                        principalTable: "clients",
                        principalColumn: "id_client",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clinical_notes",
                columns: table => new
                {
                    id_note = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_client = table.Column<int>(type: "int", nullable: false),
                    id_professional = table.Column<int>(type: "int", nullable: false),
                    id_appointment = table.Column<int>(type: "int", nullable: true),
                    encrypted_content = table.Column<string>(type: "longtext", maxLength: 60000, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinical_notes", x => x.id_note);
                    table.ForeignKey(
                        name: "FK_clinical_notes_appointments_id_appointment",
                        column: x => x.id_appointment,
                        principalTable: "appointments",
                        principalColumn: "id_appointment",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_clinical_notes_clients_id_client",
                        column: x => x.id_client,
                        principalTable: "clients",
                        principalColumn: "id_client",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clinical_notes_professionals_id_professional",
                        column: x => x.id_professional,
                        principalTable: "professionals",
                        principalColumn: "id_professional",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_clients_id_responsible_professional",
                table: "clients",
                column: "id_responsible_professional");

            migrationBuilder.CreateIndex(
                name: "IX_client_documents_id_client",
                table: "client_documents",
                column: "id_client");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_notes_id_appointment",
                table: "clinical_notes",
                column: "id_appointment");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_notes_id_client",
                table: "clinical_notes",
                column: "id_client");

            migrationBuilder.CreateIndex(
                name: "IX_clinical_notes_id_professional",
                table: "clinical_notes",
                column: "id_professional");

            migrationBuilder.AddForeignKey(
                name: "FK_clients_professionals_id_responsible_professional",
                table: "clients",
                column: "id_responsible_professional",
                principalTable: "professionals",
                principalColumn: "id_professional",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clients_professionals_id_responsible_professional",
                table: "clients");

            migrationBuilder.DropTable(
                name: "client_documents");

            migrationBuilder.DropTable(
                name: "clinical_notes");

            migrationBuilder.DropIndex(
                name: "IX_clients_id_responsible_professional",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "address",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "health_insurance",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "id_responsible_professional",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "status",
                table: "clients");
        }
    }
}
