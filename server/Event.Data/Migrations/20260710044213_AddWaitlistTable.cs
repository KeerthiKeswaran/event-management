using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Waitlists",
                columns: table => new
                {
                    Waitlist_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Attendee_Id = table.Column<int>(type: "integer", nullable: false),
                    Tier_Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Joined_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notified_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Expires_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Booking_Id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waitlists", x => x.Waitlist_Id);
                    table.ForeignKey(
                        name: "FK_Waitlists_Bookings_Booking_Id",
                        column: x => x.Booking_Id,
                        principalTable: "Bookings",
                        principalColumn: "Booking_Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Waitlists_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Waitlists_Users_Attendee_Id",
                        column: x => x.Attendee_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Waitlists_Attendee_Id",
                table: "Waitlists",
                column: "Attendee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Waitlists_Booking_Id",
                table: "Waitlists",
                column: "Booking_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Waitlists_Event_Id_Tier_Name_Status_Position",
                table: "Waitlists",
                columns: new[] { "Event_Id", "Tier_Name", "Status", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Waitlists");
        }
    }
}
