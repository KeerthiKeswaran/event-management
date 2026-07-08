using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Admin_Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Password_Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Admin_Id);
                });

            migrationBuilder.CreateTable(
                name: "Management",
                columns: table => new
                {
                    Region_Id = table.Column<string>(type: "text", nullable: false),
                    No_Of_Staffs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Management", x => x.Region_Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Transaction_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sender_Id = table.Column<string>(type: "text", nullable: false),
                    Receiver_Id = table.Column<string>(type: "text", nullable: false),
                    Stripe_Sender_Id = table.Column<string>(type: "text", nullable: true),
                    Stripe_Receiver_Id = table.Column<string>(type: "text", nullable: true),
                    Transaction_Type = table.Column<string>(type: "text", nullable: false),
                    Related_Id = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Payment_Method_Details = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Refunded_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Transaction_Reference = table.Column<string>(type: "text", nullable: true),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Transaction_Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    User_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Mobile_Number = table.Column<string>(type: "text", nullable: false),
                    Password_Hash = table.Column<string>(type: "text", nullable: false),
                    Has_Terms_Consent = table.Column<bool>(type: "boolean", nullable: false),
                    Has_Data_Share_Consent = table.Column<bool>(type: "boolean", nullable: false),
                    Has_Marketing_Consent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.User_Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Settings_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Staff_Flat_Rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Virtual_Event_Activation_Fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Physical_Event_Activation_Fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ticket_Commission_Percentage = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ticket_Fixed_Fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Max_Tickets_Per_Booking = table.Column<int>(type: "integer", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated_By_Admin_Id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Settings_Id);
                    table.ForeignKey(
                        name: "FK_PlatformSettings_Admins_Updated_By_Admin_Id",
                        column: x => x.Updated_By_Admin_Id,
                        principalTable: "Admins",
                        principalColumn: "Admin_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Employee_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region_Id = table.Column<string>(type: "text", nullable: false),
                    IsAllocated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.Employee_ID);
                    table.ForeignKey(
                        name: "FK_Staffs_Management_Region_Id",
                        column: x => x.Region_Id,
                        principalTable: "Management",
                        principalColumn: "Region_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Venue_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Region_Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Hourly_Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Is_Available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Venue_Id);
                    table.ForeignKey(
                        name: "FK_Venues_Management_Region_Id",
                        column: x => x.Region_Id,
                        principalTable: "Management",
                        principalColumn: "Region_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportQueries",
                columns: table => new
                {
                    Query_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User_Id = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportQueries", x => x.Query_Id);
                    table.ForeignKey(
                        name: "FK_SupportQueries_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInterestedRegions",
                columns: table => new
                {
                    User_Id = table.Column<int>(type: "integer", nullable: false),
                    Region_Id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterestedRegions", x => new { x.User_Id, x.Region_Id });
                    table.ForeignKey(
                        name: "FK_UserInterestedRegions_Management_Region_Id",
                        column: x => x.Region_Id,
                        principalTable: "Management",
                        principalColumn: "Region_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInterestedRegions_Users_User_Id",
                        column: x => x.User_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Event_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Organizer_Id = table.Column<int>(type: "integer", nullable: false),
                    Venue_Id = table.Column<int>(type: "integer", nullable: true),
                    Event_Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Date_Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration_Hours = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Requires_Staff = table.Column<bool>(type: "boolean", nullable: false),
                    Virtual_Url = table.Column<string>(type: "text", nullable: true),
                    Virtual_Password_Hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Event_Id);
                    table.ForeignKey(
                        name: "FK_Events_Users_Organizer_Id",
                        column: x => x.Organizer_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Venues_Venue_Id",
                        column: x => x.Venue_Id,
                        principalTable: "Venues",
                        principalColumn: "Venue_Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VenueSeatCapacities",
                columns: table => new
                {
                    Venue_Id = table.Column<int>(type: "integer", nullable: false),
                    Tier_Name = table.Column<string>(type: "text", nullable: false),
                    Total_Seats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSeatCapacities", x => new { x.Venue_Id, x.Tier_Name });
                    table.ForeignKey(
                        name: "FK_VenueSeatCapacities_Venues_Venue_Id",
                        column: x => x.Venue_Id,
                        principalTable: "Venues",
                        principalColumn: "Venue_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Booking_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Attendee_Id = table.Column<int>(type: "integer", nullable: false),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Booking_Status = table.Column<string>(type: "text", nullable: false),
                    Qr_Code_Path = table.Column<string>(type: "text", nullable: true),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Booking_Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_Attendee_Id",
                        column: x => x.Attendee_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventFeedbacks",
                columns: table => new
                {
                    Feedback_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Attendee_Id = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Review = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventFeedbacks", x => x.Feedback_Id);
                    table.ForeignKey(
                        name: "FK_EventFeedbacks_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventFeedbacks_Users_Attendee_Id",
                        column: x => x.Attendee_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventReports",
                columns: table => new
                {
                    Report_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Reporter_Id = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReports", x => x.Report_Id);
                    table.ForeignKey(
                        name: "FK_EventReports_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventReports_Users_Reporter_Id",
                        column: x => x.Reporter_Id,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventStaffAllocations",
                columns: table => new
                {
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Employee_ID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStaffAllocations", x => new { x.Event_Id, x.Employee_ID });
                    table.ForeignKey(
                        name: "FK_EventStaffAllocations_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventStaffAllocations_Staffs_Employee_ID",
                        column: x => x.Employee_ID,
                        principalTable: "Staffs",
                        principalColumn: "Employee_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTiers",
                columns: table => new
                {
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Tier_Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tickets_Sold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTiers", x => new { x.Event_Id, x.Tier_Name });
                    table.ForeignKey(
                        name: "FK_EventTicketTiers_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizerPayouts",
                columns: table => new
                {
                    Payout_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Transaction_Id = table.Column<int>(type: "integer", nullable: true),
                    Total_Ticket_Sales = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Platform_Commission = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payout_Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payout_Status = table.Column<string>(type: "text", nullable: false),
                    Processed_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerPayouts", x => x.Payout_Id);
                    table.ForeignKey(
                        name: "FK_OrganizerPayouts_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizerPayouts_Transactions_Transaction_Id",
                        column: x => x.Transaction_Id,
                        principalTable: "Transactions",
                        principalColumn: "Transaction_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizerUpfrontPayments",
                columns: table => new
                {
                    Upfront_Payment_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Event_Id = table.Column<int>(type: "integer", nullable: false),
                    Transaction_Id = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payment_Status = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizerUpfrontPayments", x => x.Upfront_Payment_Id);
                    table.ForeignKey(
                        name: "FK_OrganizerUpfrontPayments_Events_Event_Id",
                        column: x => x.Event_Id,
                        principalTable: "Events",
                        principalColumn: "Event_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizerUpfrontPayments_Transactions_Transaction_Id",
                        column: x => x.Transaction_Id,
                        principalTable: "Transactions",
                        principalColumn: "Transaction_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingDetails",
                columns: table => new
                {
                    Booking_Id = table.Column<int>(type: "integer", nullable: false),
                    Tier_Name = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDetails", x => new { x.Booking_Id, x.Tier_Name });
                    table.ForeignKey(
                        name: "FK_BookingDetails_Bookings_Booking_Id",
                        column: x => x.Booking_Id,
                        principalTable: "Bookings",
                        principalColumn: "Booking_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPayments",
                columns: table => new
                {
                    Booking_Payment_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Booking_Id = table.Column<int>(type: "integer", nullable: false),
                    Transaction_Id = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Platform_Fee_Cut = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payment_Status = table.Column<string>(type: "text", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayments", x => x.Booking_Payment_Id);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Bookings_Booking_Id",
                        column: x => x.Booking_Id,
                        principalTable: "Bookings",
                        principalColumn: "Booking_Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayments_Transactions_Transaction_Id",
                        column: x => x.Transaction_Id,
                        principalTable: "Transactions",
                        principalColumn: "Transaction_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_Booking_Id",
                table: "BookingPayments",
                column: "Booking_Id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_Transaction_Id",
                table: "BookingPayments",
                column: "Transaction_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Attendee_Id",
                table: "Bookings",
                column: "Attendee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Event_Id",
                table: "Bookings",
                column: "Event_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventFeedbacks_Attendee_Id",
                table: "EventFeedbacks",
                column: "Attendee_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventFeedbacks_Event_Id",
                table: "EventFeedbacks",
                column: "Event_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventReports_Event_Id",
                table: "EventReports",
                column: "Event_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventReports_Reporter_Id",
                table: "EventReports",
                column: "Reporter_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Organizer_Id",
                table: "Events",
                column: "Organizer_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Venue_Id",
                table: "Events",
                column: "Venue_Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventStaffAllocations_Employee_ID",
                table: "EventStaffAllocations",
                column: "Employee_ID");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerPayouts_Event_Id",
                table: "OrganizerPayouts",
                column: "Event_Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerPayouts_Transaction_Id",
                table: "OrganizerPayouts",
                column: "Transaction_Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerUpfrontPayments_Event_Id",
                table: "OrganizerUpfrontPayments",
                column: "Event_Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizerUpfrontPayments_Transaction_Id",
                table: "OrganizerUpfrontPayments",
                column: "Transaction_Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSettings_Updated_By_Admin_Id",
                table: "PlatformSettings",
                column: "Updated_By_Admin_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Region_Id",
                table: "Staffs",
                column: "Region_Id");

            migrationBuilder.CreateIndex(
                name: "IX_SupportQueries_User_Id",
                table: "SupportQueries",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterestedRegions_Region_Id",
                table: "UserInterestedRegions",
                column: "Region_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Region_Id",
                table: "Venues",
                column: "Region_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingDetails");

            migrationBuilder.DropTable(
                name: "BookingPayments");

            migrationBuilder.DropTable(
                name: "EventFeedbacks");

            migrationBuilder.DropTable(
                name: "EventReports");

            migrationBuilder.DropTable(
                name: "EventStaffAllocations");

            migrationBuilder.DropTable(
                name: "EventTicketTiers");

            migrationBuilder.DropTable(
                name: "OrganizerPayouts");

            migrationBuilder.DropTable(
                name: "OrganizerUpfrontPayments");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropTable(
                name: "SupportQueries");

            migrationBuilder.DropTable(
                name: "UserInterestedRegions");

            migrationBuilder.DropTable(
                name: "VenueSeatCapacities");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "Management");
        }
    }
}
