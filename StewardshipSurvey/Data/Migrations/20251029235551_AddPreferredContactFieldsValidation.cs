using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredContactFieldsValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredContactEmail",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrefersEmail",
                table: "MemberInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrefersPhone",
                table: "MemberInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrefersText",
                table: "MemberInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredContactEmail",
                table: "MemberInfos");

            migrationBuilder.DropColumn(
                name: "PrefersEmail",
                table: "MemberInfos");

            migrationBuilder.DropColumn(
                name: "PrefersPhone",
                table: "MemberInfos");

            migrationBuilder.DropColumn(
                name: "PrefersText",
                table: "MemberInfos");
        }
    }
}
