using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StewardshipSurvey.Data.Migrations
{
    /// <summary>
    /// Drops three columns that carried no information.
    /// <para>
    /// <c>MemberInterests.MemberInterestID</c> and <c>MemberInvolvements.MemberInvolvementID</c>
    /// were surrogate keys declared with <c>[Key]</c> on the entities, but the fluent
    /// <c>HasKey</c> in <c>ApplicationDbContext</c> overrode both with the composite key, so EF
    /// never wrote them and every row held 0. <c>MemberServiceRole</c> never had them, which is
    /// the shape the other two now match.
    /// </para>
    /// <para>
    /// <c>MemberInfos.ApplicationUserID</c> looked like a foreign key and was not one - the
    /// real link is <c>AspNetUsers.MemberID</c>. Nothing ever assigned it, so every row held
    /// the empty string. It was created by renaming <c>PhoneNumber</c> in
    /// <c>20250914213506_MemberInfo</c>, which is how a column nobody wanted acquired a name
    /// that sounded load-bearing.
    /// </para>
    /// <para>
    /// EF warns that this may lose data. It does not: all three columns are constant. The
    /// warning is worth recording rather than suppressing, because nothing in the test suite
    /// exercises migrations - <c>EnsureCreated</c> builds the test schema from the model - so
    /// this file has been read rather than proven.
    /// </para>
    /// </summary>
    public partial class DropUnusedMemberColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberInvolvementID",
                table: "MemberInvolvements");

            migrationBuilder.DropColumn(
                name: "MemberInterestID",
                table: "MemberInterests");

            migrationBuilder.DropColumn(
                name: "ApplicationUserID",
                table: "MemberInfos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MemberInvolvementID",
                table: "MemberInvolvements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MemberInterestID",
                table: "MemberInterests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserID",
                table: "MemberInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
