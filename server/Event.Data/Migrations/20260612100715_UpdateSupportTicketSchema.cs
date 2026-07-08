using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupportTicketSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Response",
                table: "SupportTickets");

            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "SupportTickets",
                newName: "RequestType");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "SupportTickets",
                newName: "ConcernUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestType",
                table: "SupportTickets",
                newName: "Subject");

            migrationBuilder.RenameColumn(
                name: "ConcernUrl",
                table: "SupportTickets",
                newName: "Message");

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "SupportTickets",
                type: "text",
                nullable: true);
        }
    }
}
