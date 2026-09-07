using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectedIntersts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberInterests_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                table: "MemberInterests");

            migrationBuilder.DropIndex(
                name: "IX_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                table: "MemberInterests");

            migrationBuilder.DropColumn(
                name: "MemberInterestInterestAreaID",
                table: "MemberInterests");

            migrationBuilder.DropColumn(
                name: "MemberInterestMemberID",
                table: "MemberInterests");

            migrationBuilder.AddColumn<int>(
                name: "MemberInterestID",
                table: "MemberInterests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberInterestID",
                table: "MemberInterests");

            migrationBuilder.AddColumn<int>(
                name: "MemberInterestInterestAreaID",
                table: "MemberInterests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberInterestMemberID",
                table: "MemberInterests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                table: "MemberInterests",
                columns: new[] { "MemberInterestMemberID", "MemberInterestInterestAreaID" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberInterests_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                table: "MemberInterests",
                columns: new[] { "MemberInterestMemberID", "MemberInterestInterestAreaID" },
                principalTable: "MemberInterests",
                principalColumns: new[] { "MemberID", "InterestAreaID" });
        }
    }
}
