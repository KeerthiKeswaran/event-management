using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketIdToAdminAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "AdminActions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminActions_TicketId",
                table: "AdminActions",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminActions_SupportTickets_TicketId",
                table: "AdminActions",
                column: "TicketId",
                principalTable: "SupportTickets",
                principalColumn: "Ticket_Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminActions_SupportTickets_TicketId",
                table: "AdminActions");

            migrationBuilder.DropIndex(
                name: "IX_AdminActions_TicketId",
                table: "AdminActions");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "AdminActions");
        }
    }
}
