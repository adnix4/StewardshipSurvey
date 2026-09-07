using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectMemberServiceRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberServiceRoles_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberServiceRoles_ServiceRoles_ServiceRoleID",
                table: "MemberServiceRoles");

            migrationBuilder.DropTable(
                name: "ServiceRoles");

            migrationBuilder.DropIndex(
                name: "IX_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles");

            migrationBuilder.DropColumn(
                name: "MemberServiceRoleMemberID",
                table: "MemberServiceRoles");

            migrationBuilder.DropColumn(
                name: "MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles");

            migrationBuilder.RenameColumn(
                name: "ServiceRoleID",
                table: "MemberServiceRoles",
                newName: "InvolvementAreaID");

            migrationBuilder.RenameIndex(
                name: "IX_MemberServiceRoles_ServiceRoleID",
                table: "MemberServiceRoles",
                newName: "IX_MemberServiceRoles_InvolvementAreaID");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberServiceRoles_InvolvementAreas_InvolvementAreaID",
                table: "MemberServiceRoles",
                column: "InvolvementAreaID",
                principalTable: "InvolvementAreas",
                principalColumn: "InvolvementAreaID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberServiceRoles_InvolvementAreas_InvolvementAreaID",
                table: "MemberServiceRoles");

            migrationBuilder.RenameColumn(
                name: "InvolvementAreaID",
                table: "MemberServiceRoles",
                newName: "ServiceRoleID");

            migrationBuilder.RenameIndex(
                name: "IX_MemberServiceRoles_InvolvementAreaID",
                table: "MemberServiceRoles",
                newName: "IX_MemberServiceRoles_ServiceRoleID");

            migrationBuilder.AddColumn<int>(
                name: "MemberServiceRoleMemberID",
                table: "MemberServiceRoles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceRoles",
                columns: table => new
                {
                    ServiceRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ServiceRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRoles", x => x.ServiceRoleID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles",
                columns: new[] { "MemberServiceRoleMemberID", "MemberServiceRoleServiceRoleID" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberServiceRoles_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles",
                columns: new[] { "MemberServiceRoleMemberID", "MemberServiceRoleServiceRoleID" },
                principalTable: "MemberServiceRoles",
                principalColumns: new[] { "MemberID", "ServiceRoleID" });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberServiceRoles_ServiceRoles_ServiceRoleID",
                table: "MemberServiceRoles",
                column: "ServiceRoleID",
                principalTable: "ServiceRoles",
                principalColumn: "ServiceRoleID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
