using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminActionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminActions_Admins_Admin_Id",
                table: "AdminActions");

            migrationBuilder.RenameColumn(
                name: "Target_Type",
                table: "AdminActions",
                newName: "TargetType");

            migrationBuilder.RenameColumn(
                name: "Target_Id",
                table: "AdminActions",
                newName: "TargetId");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "AdminActions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Admin_Id",
                table: "AdminActions",
                newName: "AdminId");

            migrationBuilder.RenameColumn(
                name: "Action_Type",
                table: "AdminActions",
                newName: "ActionType");

            migrationBuilder.RenameColumn(
                name: "Action_Id",
                table: "AdminActions",
                newName: "ActionId");

            migrationBuilder.RenameIndex(
                name: "IX_AdminActions_Admin_Id",
                table: "AdminActions",
                newName: "IX_AdminActions_AdminId");

            migrationBuilder.AddColumn<int>(
                name: "ReferenceId",
                table: "AdminActions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminActions_Admins_AdminId",
                table: "AdminActions",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "Admin_Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminActions_Admins_AdminId",
                table: "AdminActions");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "AdminActions");

            migrationBuilder.RenameColumn(
                name: "TargetType",
                table: "AdminActions",
                newName: "Target_Type");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "AdminActions",
                newName: "Target_Id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AdminActions",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "AdminActions",
                newName: "Admin_Id");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "AdminActions",
                newName: "Action_Type");

            migrationBuilder.RenameColumn(
                name: "ActionId",
                table: "AdminActions",
                newName: "Action_Id");

            migrationBuilder.RenameIndex(
                name: "IX_AdminActions_AdminId",
                table: "AdminActions",
                newName: "IX_AdminActions_Admin_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminActions_Admins_Admin_Id",
                table: "AdminActions",
                column: "Admin_Id",
                principalTable: "Admins",
                principalColumn: "Admin_Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
