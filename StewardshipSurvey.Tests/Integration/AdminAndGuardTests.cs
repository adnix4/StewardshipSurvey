using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Four unrelated correctness fixes that share a fixture: the admin edit page reporting
    /// failures instead of redirecting as success, a role change ending the target's stale
    /// session, the service-roles step guarding its POST as well as its GET, and the API
    /// controllers that were anonymous-by-default.
    /// </summary>
    public class AdminAndGuardTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public AdminAndGuardTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        [Fact]
        public async Task A_role_change_ends_the_targets_existing_session()
        {
            // Role claims live in the auth cookie and in any bearer token already issued.
            // Changing the role store alone left both carrying the old roles - cosmetically in
            // the nav, and substantively for an API token, which kept the old roles until it
            // expired.
            var target = Unique("promoted");
            var user = await _factory.CreateUserAsync(target, status: MembershipStatus.Member);

            var token = await _factory.IssueTokenAsync(target);
            Assert.Equal(HttpStatusCode.OK,
                (await _factory.CreateClientWithToken(token).GetAsync("/api/members/current")).StatusCode);

            var before = await SecurityStampAsync(user.Id);
            await EditUserAsync(user.Id, new[] { Roles.Staff }, MembershipStatus.Member);
            var after = await SecurityStampAsync(user.Id);

            Assert.NotEqual(before, after);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await _factory.CreateClientWithToken(token).GetAsync("/api/members/current")).StatusCode);
        }

        [Fact]
        public async Task An_edit_that_changes_nothing_leaves_the_session_alone()
        {
            // The stamp bump has a real cost - it signs the person out everywhere - so it must
            // not fire when an admin opens the page and saves without changing anything.
            var target = Unique("untouched");
            var user = await _factory.CreateUserAsync(target, status: MembershipStatus.Member);

            var before = await SecurityStampAsync(user.Id);
            await EditUserAsync(user.Id, Array.Empty<string>(), MembershipStatus.Member);
            var after = await SecurityStampAsync(user.Id);

            Assert.Equal(before, after);
        }

        [Fact]
        public async Task A_prospective_member_cannot_post_to_the_service_roles_step()
        {
            // The GET redirected them away; the POST did not, so the URL was enough to save
            // answers to a step that does not apply to them.
            var email = Unique("prospect");
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.ProspectiveMember);
            var client = await _factory.CreateSignedInClientAsync(email);

            // The form is unreachable for this account, so borrow a token from a page that
            // renders one.
            var page = await client.GetStringAsync("/Members/SelectInterests");

            var response = await client.PostAsync("/Members/SelectMemberServiceRoles",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("SelectedInvolvementIds", "1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", AntiforgeryToken.Extract(page))
                }));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Contains("/Members/MemberInfo", response.Headers.Location?.OriginalString);

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                Assert.Empty(await context.MemberServiceRoles
                    .Where(msr => msr.MemberID == user.MemberID!.Value)
                    .ToListAsync());
            });
        }

        [Theory]
        [InlineData("/api/interests/current")]
        [InlineData("/api/involvements/current")]
        [InlineData("/api/serviceroles/current")]
        public async Task The_per_member_endpoints_refuse_an_anonymous_caller(string path)
        {
            var response = await _factory.CreateNonRedirectingClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/interests/all")]
        [InlineData("/api/involvements/all")]
        [InlineData("/api/serviceroles/all")]
        public async Task The_catalogue_endpoints_stay_public(string path)
        {
            // Class-level [Authorize] must not have quietly closed the lists the survey needs
            // before anyone signs in. These opt back out with [AllowAnonymous].
            var response = await _factory.CreateNonRedirectingClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task The_seeder_repairs_a_member_role_left_without_a_status()
        {
            // The combination MembershipStatusRoles.SyncAsync can never produce and nothing
            // repaired: the Member role with a null MembershipStatus. Two live accounts had it.
            var email = Unique("drifted");
            var user = await _factory.CreateUserAsync(email);

            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var stored = await userManager.FindByIdAsync(user.Id);

                await userManager.AddToRoleAsync(stored!, Roles.Member);
                Assert.True(await AdminSeeder.StatusRolesAreStaleAsync(userManager, stored!, null));

                await MembershipStatusRoles.SyncAsync(userManager, stored!, null);

                Assert.False(await userManager.IsInRoleAsync(stored!, Roles.Member));
                Assert.False(await AdminSeeder.StatusRolesAreStaleAsync(userManager, stored!, null));
            });
        }

        [Fact]
        public async Task A_matching_status_role_is_not_reported_stale()
        {
            // The guard on the test above: if this returned true for everyone, the seeder would
            // rewrite every account on every start and the log count would be meaningless.
            var email = Unique("consistent");
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var stored = await userManager.FindByIdAsync(user.Id);

                Assert.False(await AdminSeeder.StatusRolesAreStaleAsync(
                    userManager, stored!, MembershipStatus.Member));
            });
        }

        private async Task<string> SecurityStampAsync(string userId)
        {
            var stamp = string.Empty;

            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                stamp = await userManager.GetSecurityStampAsync(user!);
            });

            return stamp;
        }

        private async Task EditUserAsync(string userId, string[] roles, MembershipStatus? status)
        {
            var admin = Unique("admin");
            await _factory.CreateUserAsync(admin, status: MembershipStatus.Member, roles: Roles.Admin);
            var client = await _factory.CreateSignedInClientAsync(admin);

            var page = await client.GetStringAsync($"/Admin/EditUser?id={userId}");

            var fields = new List<KeyValuePair<string, string>>
            {
                new("id", userId),
                new("membershipStatus", ((int)status!).ToString()),
                new("__RequestVerificationToken", AntiforgeryToken.Extract(page))
            };

            fields.AddRange(roles.Select(r => new KeyValuePair<string, string>("selectedRoles", r)));

            var response = await client.PostAsync($"/Admin/EditUser?id={userId}",
                new FormUrlEncodedContent(fields));

            Assert.True(response.StatusCode == HttpStatusCode.Found,
                $"Edit returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
