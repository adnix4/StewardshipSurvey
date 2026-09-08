using System.Net;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// The CSV export off the staff member report. Its filter chain used to be a third copy of
    /// the one in <c>ReportsController</c> and <c>OnGetAsync</c>, and the copies had drifted:
    /// this handler accepted <c>sortColumn</c> and <c>sortAscending</c> and then never sorted,
    /// so the file came out in database order while the screen above showed the same filters
    /// sorted. It also carried a Status column that could only ever say "Active".
    /// </summary>
    public class MemberReportExportTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public MemberReportExportTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        [Fact]
        public async Task The_export_honours_the_sort_it_is_given()
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedMemberAsync($"Zoe{tag}", "Zeta");
            await SeedMemberAsync($"Adam{tag}", "Alpha");

            var csv = await ExportAsync($"searchTerm={tag}&sortColumn=FirstName&sortAscending=true");
            var descending = await ExportAsync($"searchTerm={tag}&sortColumn=FirstName&sortAscending=false");

            Assert.True(csv.IndexOf($"Adam{tag}") < csv.IndexOf($"Zoe{tag}"),
                "Ascending export did not put Adam before Zoe.");
            Assert.True(descending.IndexOf($"Zoe{tag}") < descending.IndexOf($"Adam{tag}"),
                "Descending export did not put Zoe before Adam - the sort argument was ignored.");
        }

        [Fact]
        public async Task The_export_has_no_status_column()
        {
            // It read member.IsActive after the query had already filtered to active members,
            // so every row of every export ever taken said "Active".
            var csv = await ExportAsync("");
            var header = csv.Split('\n')[0].Trim();

            Assert.DoesNotContain("Status", header);
            Assert.Equal(7, header.Split(',').Length);
        }

        [Fact]
        public async Task The_export_applies_the_search_filter()
        {
            // Guards the extraction: all three call sites now share one filter chain, so a
            // mistake in it would silently widen or narrow what staff can export.
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedMemberAsync($"Wanted{tag}", "Match");
            await SeedMemberAsync("Unwanted", "Nomatch");

            var csv = await ExportAsync($"searchTerm={tag}");

            Assert.Contains($"Wanted{tag}", csv);
            Assert.DoesNotContain("Unwanted", csv);
        }

        [Fact]
        public async Task The_export_and_the_api_report_agree_on_who_is_included()
        {
            // The two used to be independent copies. If they disagree now, the shared chain is
            // being called differently from one of them.
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedMemberAsync($"Both{tag}", "Included");

            var staff = Unique("reportstaff");
            await _factory.CreateUserAsync(staff, status: MembershipStatus.Member, roles: Roles.Staff);
            var client = await _factory.CreateSignedInClientAsync(staff);

            var csv = await client.GetStringAsync($"/Staff/MemberReport?handler=Export&searchTerm={tag}");
            var json = await client.GetStringAsync($"/api/reports/members?searchTerm={tag}");

            Assert.Contains($"Both{tag}", csv);
            Assert.Contains($"Both{tag}", json);
        }

        [Fact]
        public async Task A_member_cannot_export_the_report()
        {
            var email = Unique("nosy");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            var response = await client.GetAsync("/Staff/MemberReport?handler=Export");

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<string> ExportAsync(string query)
        {
            var staff = Unique("exporter");
            await _factory.CreateUserAsync(staff, status: MembershipStatus.Member, roles: Roles.Staff);
            var client = await _factory.CreateSignedInClientAsync(staff);

            var response = await client.GetAsync($"/Staff/MemberReport?handler=Export&{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.Content.ReadAsStringAsync();
        }

        private async Task SeedMemberAsync(string firstName, string lastName) =>
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                context.MemberInfos.Add(new MemberInfo
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName}@stmark.local",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    MembershipStatus = MembershipStatus.Member
                });

                await context.SaveChangesAsync();
            });
    }
}
