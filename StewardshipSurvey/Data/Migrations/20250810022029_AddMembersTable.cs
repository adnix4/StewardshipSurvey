using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMembersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterestsAreas",
                columns: table => new
                {
                    InterestAreaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterestArea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestsAreas", x => x.InterestAreaID);
                });

            migrationBuilder.CreateTable(
                name: "InvolvementAreas",
                columns: table => new
                {
                    InvolvementAreaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaOfInvolvement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvolvementAreas", x => x.InvolvementAreaID);
                });

            migrationBuilder.CreateTable(
                name: "MemberInfos",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberInfos", x => x.MemberID);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRoles",
                columns: table => new
                {
                    ServiceRoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRoles", x => x.ServiceRoleID);
                });

            migrationBuilder.CreateTable(
                name: "MemberInterests",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    InterestAreaID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberInterestInterestAreaID = table.Column<int>(type: "int", nullable: true),
                    MemberInterestMemberID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberInterests", x => new { x.MemberID, x.InterestAreaID });
                    table.ForeignKey(
                        name: "FK_MemberInterests_InterestsAreas_InterestAreaID",
                        column: x => x.InterestAreaID,
                        principalTable: "InterestsAreas",
                        principalColumn: "InterestAreaID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberInterests_MemberInfos_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MemberInfos",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberInterests_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                        columns: x => new { x.MemberInterestMemberID, x.MemberInterestInterestAreaID },
                        principalTable: "MemberInterests",
                        principalColumns: new[] { "MemberID", "InterestAreaID" });
                });

            migrationBuilder.CreateTable(
                name: "MemberInvolvements",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    InvolvementAreaID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberInvolvementInvolvementAreaID = table.Column<int>(type: "int", nullable: true),
                    MemberInvolvementMemberID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberInvolvements", x => new { x.MemberID, x.InvolvementAreaID });
                    table.ForeignKey(
                        name: "FK_MemberInvolvements_InvolvementAreas_InvolvementAreaID",
                        column: x => x.InvolvementAreaID,
                        principalTable: "InvolvementAreas",
                        principalColumn: "InvolvementAreaID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberInvolvements_MemberInfos_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MemberInfos",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberInvolvements_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                        columns: x => new { x.MemberInvolvementMemberID, x.MemberInvolvementInvolvementAreaID },
                        principalTable: "MemberInvolvements",
                        principalColumns: new[] { "MemberID", "InvolvementAreaID" });
                });

            migrationBuilder.CreateTable(
                name: "MemberServiceRoles",
                columns: table => new
                {
                    MemberID = table.Column<int>(type: "int", nullable: false),
                    ServiceRoleID = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberServiceRoleMemberID = table.Column<int>(type: "int", nullable: true),
                    MemberServiceRoleServiceRoleID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberServiceRoles", x => new { x.MemberID, x.ServiceRoleID });
                    table.ForeignKey(
                        name: "FK_MemberServiceRoles_MemberInfos_MemberID",
                        column: x => x.MemberID,
                        principalTable: "MemberInfos",
                        principalColumn: "MemberID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberServiceRoles_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                        columns: x => new { x.MemberServiceRoleMemberID, x.MemberServiceRoleServiceRoleID },
                        principalTable: "MemberServiceRoles",
                        principalColumns: new[] { "MemberID", "ServiceRoleID" });
                    table.ForeignKey(
                        name: "FK_MemberServiceRoles_ServiceRoles_ServiceRoleID",
                        column: x => x.ServiceRoleID,
                        principalTable: "ServiceRoles",
                        principalColumn: "ServiceRoleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberInterests_InterestAreaID",
                table: "MemberInterests",
                column: "InterestAreaID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberInterests_MemberInterestMemberID_MemberInterestInterestAreaID",
                table: "MemberInterests",
                columns: new[] { "MemberInterestMemberID", "MemberInterestInterestAreaID" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvolvements_InvolvementAreaID",
                table: "MemberInvolvements",
                column: "InvolvementAreaID");

            migrationBuilder.CreateIndex(
                name: "IX_MemberInvolvements_MemberInvolvementMemberID_MemberInvolvementInvolvementAreaID",
                table: "MemberInvolvements",
                columns: new[] { "MemberInvolvementMemberID", "MemberInvolvementInvolvementAreaID" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberServiceRoles_MemberServiceRoleMemberID_MemberServiceRoleServiceRoleID",
                table: "MemberServiceRoles",
                columns: new[] { "MemberServiceRoleMemberID", "MemberServiceRoleServiceRoleID" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberServiceRoles_ServiceRoleID",
                table: "MemberServiceRoles",
                column: "ServiceRoleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberInterests");

            migrationBuilder.DropTable(
                name: "MemberInvolvements");

            migrationBuilder.DropTable(
                name: "MemberServiceRoles");

            migrationBuilder.DropTable(
                name: "InterestsAreas");

            migrationBuilder.DropTable(
                name: "InvolvementAreas");

            migrationBuilder.DropTable(
                name: "MemberInfos");

            migrationBuilder.DropTable(
                name: "ServiceRoles");
        }
    }
}
