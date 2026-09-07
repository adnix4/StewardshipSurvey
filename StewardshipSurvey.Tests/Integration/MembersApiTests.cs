using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Who may read a member profile through <c>GET /api/members/{id}</c>.
    /// <para>
    /// This endpoint used to carry only the class-level <c>[Authorize]</c>, which made every
    /// member's home address and birth date readable by anyone willing to register. These
    /// tests exist to keep that door shut, so treat a failure here as a disclosure bug and
    /// not a broken assertion.
    /// </para>
    /// <para>
    /// The API authenticates by cookie - no bearer scheme is registered - so every client
    /// here signs in through the real Identity UI.
    /// </para>
    /// </summary>
    public class MembersApiTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public MembersApiTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task A_member_can_read_their_own_profile_by_id()
        {
            var user = await _factory.CreateUserAsync(
                Unique("owner"), status: MembershipStatus.Member);

            await StampAddressAsync(user.MemberID!.Value, "12 Own Street");

            var client = await _factory.CreateSignedInClientAsync(user.Email!);
            var response = await client.GetAsync($"/api/members/{user.MemberID}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("12 Own Street", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_member_cannot_read_another_members_profile()
        {
            var victim = await _factory.CreateUserAsync(
                Unique("victim"), status: MembershipStatus.Member);
            await StampAddressAsync(victim.MemberID!.Value, "99 Private Lane");

            var snooper = await _factory.CreateUserAsync(
                Unique("snooper"), status: MembershipStatus.Member);

            var client = await _factory.CreateSignedInClientAsync(snooper.Email!);
            var response = await client.GetAsync($"/api/members/{victim.MemberID}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain("99 Private Lane", await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The original attack: register, then count upwards. Walking the low ids must not
        /// return anyone else's details, whatever the status code happens to be.
        /// </summary>
        [Fact]
        public async Task Walking_the_id_range_leaks_nothing()
        {
            var victim = await _factory.CreateUserAsync(
                Unique("enum-victim"), status: MembershipStatus.Member);
            await StampAddressAsync(victim.MemberID!.Value, "77 Enumerated Way");

            var snooper = await _factory.CreateUserAsync(
                Unique("enum-snooper"), status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(snooper.Email!);

            for (var id = 1; id <= victim.MemberID!.Value + 2; id++)
            {
                if (id == snooper.MemberID) continue;

                var response = await client.GetAsync($"/api/members/{id}");
                var payload = await response.Content.ReadAsStringAsync();

                Assert.False(response.IsSuccessStatusCode,
                    $"id {id} returned {(int)response.StatusCode} to a caller who does not own it.");
                Assert.DoesNotContain("77 Enumerated Way", payload);
            }
        }

        [Theory]
        [InlineData(Roles.Staff)]
        [InlineData(Roles.Admin)]
        public async Task Staff_and_admins_may_read_any_profile(string role)
        {
            var member = await _factory.CreateUserAsync(
                Unique("subject"), status: MembershipStatus.Member);
            await StampAddressAsync(member.MemberID!.Value, "3 Reportable Road");

            var reader = await _factory.CreateUserAsync(
                Unique(role.ToLowerInvariant()), status: MembershipStatus.Member, roles: role);

            var client = await _factory.CreateSignedInClientAsync(reader.Email!);
            var response = await client.GetAsync($"/api/members/{member.MemberID}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("3 Reportable Road", await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// A signed-in account with no profile of its own owns no id, so every id is somebody
        /// else's. It must not fall through to the lookup.
        /// </summary>
        [Fact]
        public async Task An_account_with_no_profile_is_refused()
        {
            var subject = await _factory.CreateUserAsync(
                Unique("profiled"), status: MembershipStatus.Member);

            var profileless = await _factory.CreateUserAsync(Unique("profileless"));

            var client = await _factory.CreateSignedInClientAsync(profileless.Email!);
            var response = await client.GetAsync($"/api/members/{subject.MemberID}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task An_anonymous_caller_is_challenged()
        {
            var response = await _factory.CreateNonRedirectingClient().GetAsync("/api/members/1");

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// The endpoint the MAUI client actually uses. Kept alongside the ownership tests so a
        /// heavy-handed fix to <c>{id}</c> cannot quietly break it.
        /// </summary>
        [Fact]
        public async Task Current_still_returns_the_callers_own_profile()
        {
            var user = await _factory.CreateUserAsync(
                Unique("current"), status: MembershipStatus.Member);
            await StampAddressAsync(user.MemberID!.Value, "5 Current Close");

            var client = await _factory.CreateSignedInClientAsync(user.Email!);
            var response = await client.GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("5 Current Close", await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// The factory shares one database across the whole class, so addresses have to be
        /// distinct per test rather than reused.
        /// </summary>
        private async Task StampAddressAsync(int memberId, string address)
        {
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var member = await context.MemberInfos.FirstAsync(m => m.MemberID == memberId);
                member.Address = address;
                await context.SaveChangesAsync();
            });
        }

        private static string Unique(string prefix) =>
            $"{prefix}-{Guid.NewGuid():N}@stmark.local";
    }
}
