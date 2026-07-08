using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Has_Data_Share_Consent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Has_Terms_Consent",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "Consented_Terms_Id",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Notification_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Recipient_Email = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Retry_Count = table.Column<int>(type: "integer", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sent_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Notification_Id);
                });

            migrationBuilder.CreateTable(
                name: "TermsAndConditions",
                columns: table => new
                {
                    Terms_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "text", nullable: false),
                    File_Path = table.Column<string>(type: "text", nullable: false),
                    Is_Active = table.Column<bool>(type: "boolean", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermsAndConditions", x => x.Terms_Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Consented_Terms_Id",
                table: "Users",
                column: "Consented_Terms_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TermsAndConditions_Consented_Terms_Id",
                table: "Users",
                column: "Consented_Terms_Id",
                principalTable: "TermsAndConditions",
                principalColumn: "Terms_Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TermsAndConditions_Consented_Terms_Id",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "TermsAndConditions");

            migrationBuilder.DropIndex(
                name: "IX_Users_Consented_Terms_Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Consented_Terms_Id",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "Has_Data_Share_Consent",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Has_Terms_Consent",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
