using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterestAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberInterests_InterestsAreas_InterestAreaID",
                table: "MemberInterests");

            migrationBuilder.DropTable(
                name: "InterestsAreas");

            migrationBuilder.CreateTable(
                name: "InterestAreas",
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
                    table.PrimaryKey("PK_InterestAreas", x => x.InterestAreaID);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberInterests_InterestAreas_InterestAreaID",
                table: "MemberInterests",
                column: "InterestAreaID",
                principalTable: "InterestAreas",
                principalColumn: "InterestAreaID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberInterests_InterestAreas_InterestAreaID",
                table: "MemberInterests");

            migrationBuilder.DropTable(
                name: "InterestAreas");

            migrationBuilder.CreateTable(
                name: "InterestsAreas",
                columns: table => new
                {
                    InterestAreaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterestArea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestsAreas", x => x.InterestAreaID);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_MemberInterests_InterestsAreas_InterestAreaID",
                table: "MemberInterests",
                column: "InterestAreaID",
                principalTable: "InterestsAreas",
                principalColumn: "InterestAreaID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
