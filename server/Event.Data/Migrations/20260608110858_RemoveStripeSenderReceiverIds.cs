using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStripeSenderReceiverIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stripe_Receiver_Id",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Stripe_Sender_Id",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Stripe_Receiver_Id",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stripe_Sender_Id",
                table: "Transactions",
                type: "text",
                nullable: true);
        }
    }
}
