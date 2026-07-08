using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Event.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TermsAndConditions_Consented_Terms_Id",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Consented_Terms_Id",
                table: "Users");

            // Drop identity column constraint before altering the column type to text
            migrationBuilder.Sql("ALTER TABLE \"TermsAndConditions\" ALTER COLUMN \"Terms_Id\" DROP IDENTITY IF EXISTS;");
            migrationBuilder.Sql("ALTER TABLE \"TermsAndConditions\" ALTER COLUMN \"Terms_Id\" TYPE text;");

            migrationBuilder.Sql("ALTER TABLE \"Users\" ALTER COLUMN \"Consented_Terms_Id\" TYPE text;");

            migrationBuilder.AddColumn<string>(
                name: "TermsAndConditionsTerms_Id",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Age_Category",
                table: "Events",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "ALL");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TermsAndConditionsTerms_Id",
                table: "Users",
                column: "TermsAndConditionsTerms_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TermsAndConditions_TermsAndConditionsTerms_Id",
                table: "Users",
                column: "TermsAndConditionsTerms_Id",
                principalTable: "TermsAndConditions",
                principalColumn: "Terms_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TermsAndConditions_TermsAndConditionsTerms_Id",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TermsAndConditionsTerms_Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAndConditionsTerms_Id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Age_Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.AlterColumn<int>(
                name: "Consented_Terms_Id",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Terms_Id",
                table: "TermsAndConditions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Npgsql:IdentitySequenceOptions", "'10000', '1', '', '', 'False', '1'")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

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
    }
}
