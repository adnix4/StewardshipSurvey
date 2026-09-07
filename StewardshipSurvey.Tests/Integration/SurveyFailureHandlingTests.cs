using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// The three survey steps used to catch every exception, log it, and return Page(). A
    /// member whose save failed got a page that looked like success, and a member whose page
    /// failed to load got an empty checkbox list - which, submitted, deleted the answers they
    /// had already given.
    /// <para>
    /// These cover the save half. Nothing here breaks the schema, so the shared fixture is
    /// safe; the load half lives in <see cref="SurveyLoadFailureTests"/>.
    /// </para>
    /// </summary>
    public class SurveySaveFailureTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        /// <summary>An id no InterestArea row will ever have, so SaveChanges hits the foreign key.</summary>
        private const int MissingAreaId = 999999;

        private readonly StewardshipWebApplicationFactory _factory;

        public SurveySaveFailureTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task A_save_that_fails_says_so_and_leaves_the_stored_answers_alone()
        {
            var email = $"savefail-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var memberId = await MemberIdForAsync(email);

            var kept = await SeedInterestAsync("Altar Guild");
            await StoreInterestAsync(memberId, kept);

            var client = await _factory.CreateSignedInClientAsync(email);
            var page = await client.GetStringAsync("/Members/SelectInterests");

            var response = await PostInterestsAsync(client, page, kept, MissingAreaId);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("We could not save your selections", html);

            // The removals and the additions are one SaveChanges, so the rollback took the
            // deletes with it. This is the answer-wiping half of the bug.
            Assert.Equal(new[] { kept }, await StoredInterestIdsAsync(memberId));
        }

        [Fact]
        public async Task A_failed_save_redisplays_what_the_member_ticked_not_what_is_stored()
        {
            // The old code redrew by calling OnGetAsync(), which overwrote the posted
            // selection with the database's copy - so a transient failure also looked as
            // though the member's edit had been thrown away.
            var email = $"redisplay-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var memberId = await MemberIdForAsync(email);

            var stored = await SeedInterestAsync("Bell Choir");
            var justTicked = await SeedInterestAsync("Coffee Hour");
            await StoreInterestAsync(memberId, stored);

            var client = await _factory.CreateSignedInClientAsync(email);
            var page = await client.GetStringAsync("/Members/SelectInterests");

            var response = await PostInterestsAsync(client, page, justTicked, MissingAreaId);
            var html = await response.Content.ReadAsStringAsync();

            Assert.True(IsChecked(html, justTicked), "The box the member ticked came back unticked.");
            Assert.False(IsChecked(html, stored), "The redisplayed form fell back to the stored answers.");
        }

        [Fact]
        public async Task A_post_that_repeats_an_id_still_saves()
        {
            // The key is (MemberID, InterestAreaID). Without Distinct a repeated id fails on
            // the primary key, which is nothing the member did or could correct.
            var email = $"dupe-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var memberId = await MemberIdForAsync(email);

            var area = await SeedInterestAsync("Ushering");

            var client = await _factory.CreateSignedInClientAsync(email);
            var page = await client.GetStringAsync("/Members/SelectInterests");

            var response = await PostInterestsAsync(client, page, area, area);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal(new[] { area }, await StoredInterestIdsAsync(memberId));
        }

        private static bool IsChecked(string html, int interestAreaId) =>
            Regex.IsMatch(html, $@"id=""interest_{interestAreaId}""[^>]*\schecked");

        private async Task<HttpResponseMessage> PostInterestsAsync(
            HttpClient client, string renderedPage, params int[] selectedIds)
        {
            var fields = selectedIds
                .Select(id => new KeyValuePair<string, string>("SelectedInterestIds", id.ToString()))
                .Append(new KeyValuePair<string, string>(
                    "__RequestVerificationToken", AntiforgeryToken.Extract(renderedPage)));

            return await client.PostAsync("/Members/SelectInterests", new FormUrlEncodedContent(fields));
        }

        private async Task<int> MemberIdForAsync(string email)
        {
            var memberId = 0;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                memberId = await context.MemberInfos
                    .Where(m => m.Email == email)
                    .Select(m => m.MemberID)
                    .FirstAsync();
            });

            return memberId;
        }

        private async Task<int> SeedInterestAsync(string name)
        {
            var id = 0;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var area = new InterestAreas { InterestArea = name, IsActive = true };

                context.InterestAreas.Add(area);
                await context.SaveChangesAsync();

                id = area.InterestAreaID;
            });

            return id;
        }

        private async Task StoreInterestAsync(int memberId, int interestAreaId) =>
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                context.MemberInterests.Add(new MemberInterest
                {
                    MemberID = memberId,
                    InterestAreaID = interestAreaId
                });

                await context.SaveChangesAsync();
            });

        private async Task<int[]> StoredInterestIdsAsync(int memberId)
        {
            int[] ids = Array.Empty<int>();

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                ids = await context.MemberInterests
                    .Where(mi => mi.MemberID == memberId)
                    .Select(mi => mi.InterestAreaID)
                    .OrderBy(id => id)
                    .ToArrayAsync();
            });

            return ids;
        }
    }

    /// <summary>
    /// The load half. A page whose query throws must say so and must not render the form:
    /// an empty checkbox list is indistinguishable from "I unticked everything", and posting
    /// it deletes the member's real answers.
    /// <para>
    /// Each test hides a table for the duration of one request. That is schema damage, so it
    /// is confined to this class - xUnit gives every test class its own fixture instance, and
    /// so its own SQLite database - and undone in a finally.
    /// </para>
    /// </summary>
    public class SurveyLoadFailureTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public SurveyLoadFailureTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task The_interests_page_reports_the_failure_and_renders_no_form()
        {
            var html = await LoadWithTableHiddenAsync("InterestAreas", "/Members/SelectInterests");

            Assert.Contains("We could not load your interests", html);
            Assert.DoesNotContain("name=\"SelectedInterestIds\"", html);
            Assert.DoesNotContain("Select Ministry Interests", html);
        }

        [Fact]
        public async Task The_involvement_page_reports_the_failure_and_renders_no_form()
        {
            var html = await LoadWithTableHiddenAsync("InvolvementAreas", "/Members/SelectMemberInvolvement");

            Assert.Contains("We could not load your ministries", html);
            Assert.DoesNotContain("name=\"SelectedInvolvementIds\"", html);
            Assert.DoesNotContain("Save My Service Interests", html);
        }

        [Fact]
        public async Task The_service_roles_page_reports_the_failure_and_renders_no_form()
        {
            var html = await LoadWithTableHiddenAsync("InvolvementAreas", "/Members/SelectMemberServiceRoles");

            Assert.Contains("We could not load your service roles", html);
            Assert.DoesNotContain("name=\"SelectedInvolvementIds\"", html);
            Assert.DoesNotContain("Save My Service Roles", html);
        }

        /// <summary>
        /// Signs a member in, renames <paramref name="table"/> out of the way so the page's
        /// query throws, requests <paramref name="path"/>, and puts the table back.
        /// </summary>
        private async Task<string> LoadWithTableHiddenAsync(string table, string path)
        {
            var email = $"loadfail-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            await ExecuteSqlAsync($@"ALTER TABLE ""{table}"" RENAME TO ""{table}_hidden""");
            try
            {
                var response = await client.GetAsync(path);

                // A 500 here means the handler stopped catching - which is fine for a
                // developer but not for a member halfway through the survey.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                return await response.Content.ReadAsStringAsync();
            }
            finally
            {
                await ExecuteSqlAsync($@"ALTER TABLE ""{table}_hidden"" RENAME TO ""{table}""");
            }
        }

        private async Task ExecuteSqlAsync(string sql) =>
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                await context.Database.ExecuteSqlRawAsync(sql);
            });
    }
}
