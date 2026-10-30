using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MindCare.Application.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "clients",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_email",
                table: "clients",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "notify_whatsapp",
                table: "clients",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "clients",
                type: "varchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "confirmation_token_hash",
                table: "appointments",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "patient_responded_at",
                table: "appointments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "patient_response_via",
                table: "appointments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder_queued_at",
                table: "appointments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outbound_messages",
                columns: table => new
                {
                    id_message = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_client = table.Column<int>(type: "int", nullable: false),
                    id_appointment = table.Column<int>(type: "int", nullable: true),
                    channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    recipient = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    confirmation_token = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    note = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    last_error = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    external_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbound_messages", x => x.id_message);
                    table.ForeignKey(
                        name: "FK_outbound_messages_appointments_id_appointment",
                        column: x => x.id_appointment,
                        principalTable: "appointments",
                        principalColumn: "id_appointment",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_outbound_messages_clients_id_client",
                        column: x => x.id_client,
                        principalTable: "clients",
                        principalColumn: "id_client",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "staff_notifications",
                columns: table => new
                {
                    id_notification = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_user = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    link = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    icon = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_notifications", x => x.id_notification);
                    table.ForeignKey(
                        name: "FK_staff_notifications_users_id_user",
                        column: x => x.id_user,
                        principalTable: "users",
                        principalColumn: "id_user",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_clients_phone",
                table: "clients",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_confirmation_token_hash",
                table: "appointments",
                column: "confirmation_token_hash");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_external_id",
                table: "outbound_messages",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_id_appointment",
                table: "outbound_messages",
                column: "id_appointment");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_id_client",
                table: "outbound_messages",
                column: "id_client");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_messages_status_next_attempt_at",
                table: "outbound_messages",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_notifications_id_user_read_at",
                table: "staff_notifications",
                columns: new[] { "id_user", "read_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbound_messages");

            migrationBuilder.DropTable(
                name: "staff_notifications");

            migrationBuilder.DropIndex(
                name: "IX_clients_phone",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "IX_appointments_confirmation_token_hash",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "email",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "notify_email",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "notify_whatsapp",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "confirmation_token_hash",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "patient_responded_at",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "patient_response_via",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "reminder_queued_at",
                table: "appointments");
        }
    }
}
