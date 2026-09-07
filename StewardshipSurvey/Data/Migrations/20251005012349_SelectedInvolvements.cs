using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectedInvolvements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberInvolvements_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements");

            migrationBuilder.DropIndex(
                name: "IX_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements");

            migrationBuilder.DropColumn(
                name: "MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements");

            migrationBuilder.DropColumn(
                name: "MemberInvolvementMemberID",
                table: "MemberInvolvements");

            migrationBuilder.AddColumn<int>(
                name: "MemberInvolvementID",
                table: "MemberInvolvements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberInvolvementID",
                table: "MemberInvolvements");

            migrationBuilder.AddColumn<int>(
                name: "MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberInvolvementMemberID",
                table: "MemberInvolvements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements",
                columns: new[] { "MemberInvolvementMemberID", "MemberInvolvementInvolvementAreaID" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberInvolvements_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements",
                columns: new[] { "MemberInvolvementMemberID", "MemberInvolvementInvolvementAreaID" },
                principalTable: "MemberInvolvements",
                principalColumns: new[] { "MemberID", "InvolvementAreaID" });
        }
    }
}
