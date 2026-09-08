using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// The survey endpoints the MAUI client uses: the three public catalogues, the three
    /// per-member get/save pairs, and the staff report. None of them had a test - the whole
    /// surface was written, shipped and never exercised, and could not be exercised until the
    /// bearer scheme existed.
    /// <para>
    /// The three controllers are the same shape, so most of this is a theory across the three
    /// route prefixes. Note <c>involvements</c> and <c>serviceroles</c> both draw on
    /// <c>InvolvementAreas</c> but write to different tables, which is the sort of thing three
    /// near-identical controllers get wrong quietly.
    /// </para>
    /// </summary>
    public class SurveyApiTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public SurveyApiTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task The_catalogue_is_public(string route)
        {
            // Deliberately anonymous: the survey needs these lists before anyone signs in.
            var response = await _factory.CreateNonRedirectingClient().GetAsync($"/api/{route}/all");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task The_catalogue_hides_inactive_entries(string route)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedAreaAsync(route, $"Live{tag}", isActive: true);
            await SeedAreaAsync(route, $"Retired{tag}", isActive: false);

            var body = await _factory.CreateNonRedirectingClient()
                .GetStringAsync($"/api/{route}/all");

            Assert.Contains($"Live{tag}", body);
            Assert.DoesNotContain($"Retired{tag}", body);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task The_per_member_endpoints_need_a_token(string route)
        {
            var anonymous = _factory.CreateNonRedirectingClient();

            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.GetAsync($"/api/{route}/current")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.PostAsJsonAsync($"/api/{route}/current", new[] { 1 })).StatusCode);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task Saving_answers_round_trips(string route)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            var areaId = await SeedAreaAsync(route, $"Round{tag}", isActive: true);

            var email = Unique("round");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            var saved = await client.PostAsJsonAsync($"/api/{route}/current", new[] { areaId });
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

            var body = await client.GetStringAsync($"/api/{route}/current");
            Assert.Contains($"Round{tag}", body);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task Saving_replaces_the_previous_answers(string route)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            var first = await SeedAreaAsync(route, $"First{tag}", isActive: true);
            var second = await SeedAreaAsync(route, $"Second{tag}", isActive: true);

            var email = Unique("replace");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            await client.PostAsJsonAsync($"/api/{route}/current", new[] { first });
            await client.PostAsJsonAsync($"/api/{route}/current", new[] { second });

            var body = await client.GetStringAsync($"/api/{route}/current");

            Assert.Contains($"Second{tag}", body);
            Assert.DoesNotContain($"First{tag}", body);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task An_empty_post_clears_the_answers(string route)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            var areaId = await SeedAreaAsync(route, $"Clear{tag}", isActive: true);

            var email = Unique("clear");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            await client.PostAsJsonAsync($"/api/{route}/current", new[] { areaId });
            var cleared = await client.PostAsJsonAsync($"/api/{route}/current", Array.Empty<int>());

            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            Assert.DoesNotContain($"Clear{tag}", await client.GetStringAsync($"/api/{route}/current"));
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task One_members_answers_are_not_visible_to_another(string route)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            var areaId = await SeedAreaAsync(route, $"Private{tag}", isActive: true);

            var owner = Unique("owner");
            await _factory.CreateUserAsync(owner, status: MembershipStatus.Member);
            var ownerClient = await _factory.CreateBearerClientAsync(owner);
            await ownerClient.PostAsJsonAsync($"/api/{route}/current", new[] { areaId });

            var other = Unique("other");
            await _factory.CreateUserAsync(other, status: MembershipStatus.Member);
            var otherClient = await _factory.CreateBearerClientAsync(other);

            var body = await otherClient.GetStringAsync($"/api/{route}/current");

            Assert.DoesNotContain($"Private{tag}", body);
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task A_repeated_id_does_not_fail_the_save(string route)
        {
            // The keys are composite (MemberID, AreaID). The Razor pages call Distinct() for
            // exactly this reason; these controllers did not, so a duplicated id violated the
            // primary key and surfaced as a 500.
            var tag = Guid.NewGuid().ToString("N")[..8];
            var areaId = await SeedAreaAsync(route, $"Dupe{tag}", isActive: true);

            var email = Unique("dupe");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            var response = await client.PostAsJsonAsync(
                $"/api/{route}/current", new[] { areaId, areaId });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains($"Dupe{tag}", await client.GetStringAsync($"/api/{route}/current"));
        }

        [Theory]
        [InlineData("interests")]
        [InlineData("involvements")]
        [InlineData("serviceroles")]
        public async Task An_unknown_id_is_rejected_as_a_bad_request(string route)
        {
            // Without a check this reaches the database and comes back a 500 - the caller's
            // mistake reported as the server's.
            var email = Unique("badid");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            var response = await client.PostAsJsonAsync($"/api/{route}/current", new[] { 999999 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task The_report_is_staff_only()
        {
            var member = Unique("reportmember");
            await _factory.CreateUserAsync(member, status: MembershipStatus.Member);

            Assert.Equal(HttpStatusCode.Forbidden,
                (await (await _factory.CreateBearerClientAsync(member))
                    .GetAsync("/api/reports/members")).StatusCode);
        }

        [Theory]
        [InlineData(Roles.Staff)]
        [InlineData(Roles.Admin)]
        public async Task The_report_returns_members_to_privileged_callers(string role)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedMemberAsync($"Reported{tag}");

            var email = Unique("reporter");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member, roles: role);
            var client = await _factory.CreateBearerClientAsync(email);

            var body = await client.GetStringAsync($"/api/reports/members?searchTerm={tag}");

            Assert.Contains($"Reported{tag}", body);
        }

        [Fact]
        public async Task The_report_honours_its_sort()
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            await SeedMemberAsync($"Zeta{tag}");
            await SeedMemberAsync($"Alpha{tag}");

            var email = Unique("sorter");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member, roles: Roles.Staff);
            var client = await _factory.CreateBearerClientAsync(email);

            var ascending = await client.GetStringAsync(
                $"/api/reports/members?searchTerm={tag}&sortColumn=FirstName&sortAscending=true");
            var descending = await client.GetStringAsync(
                $"/api/reports/members?searchTerm={tag}&sortColumn=FirstName&sortAscending=false");

            Assert.True(ascending.IndexOf($"Alpha{tag}") < ascending.IndexOf($"Zeta{tag}"));
            Assert.True(descending.IndexOf($"Zeta{tag}") < descending.IndexOf($"Alpha{tag}"));
        }

        /// <summary>
        /// Seeds a catalogue row for <paramref name="route"/> and returns its id.
        /// <c>involvements</c> and <c>serviceroles</c> share the InvolvementAreas table.
        /// </summary>
        private async Task<int> SeedAreaAsync(string route, string name, bool isActive)
        {
            var id = 0;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                if (route == "interests")
                {
                    var area = new InterestAreas { InterestArea = name, IsActive = isActive };
                    context.InterestAreas.Add(area);
                    await context.SaveChangesAsync();
                    id = area.InterestAreaID;
                }
                else
                {
                    var area = new InvolvementAreas { AreaOfInvolvement = name, IsActive = isActive };
                    context.InvolvementAreas.Add(area);
                    await context.SaveChangesAsync();
                    id = area.InvolvementAreaID;
                }
            });

            return id;
        }

        private async Task SeedMemberAsync(string firstName) =>
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                context.MemberInfos.Add(new MemberInfo
                {
                    FirstName = firstName,
                    LastName = "Reported",
                    Email = $"{firstName}@stmark.local",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    MembershipStatus = MembershipStatus.Member
                });

                await context.SaveChangesAsync();
            });
    }
}
