using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "MemberInfos",
                newName: "ApplicationUserID");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CellPhoneNumber",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomePhoneNumber",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPhoneNumber",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CellPhoneNumber",
                table: "MemberInfos");

            migrationBuilder.DropColumn(
                name: "HomePhoneNumber",
                table: "MemberInfos");

            migrationBuilder.DropColumn(
                name: "WorkPhoneNumber",
                table: "MemberInfos");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserID",
                table: "MemberInfos",
                newName: "PhoneNumber");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
