using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// The API issued bearer tokens that nothing ever validated. <c>Program.cs</c> registered no
    /// bearer scheme, so <c>[Authorize]</c> resolved to the Identity cookie and the
    /// <c>Authorization: Bearer</c> header the MAUI client sends on every call was ignored
    /// outright - all fifteen endpoints were unreachable from the phone.
    /// <para>
    /// These cover the scheme itself, the role claims that were missing (so the Staff-only
    /// report could never be reached with a token even in principle), and revocation - a JWT
    /// cannot be recalled, so a deactivated member kept full access until their token expired.
    /// </para>
    /// </summary>
    public class BearerAuthTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public BearerAuthTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        [Fact]
        public async Task A_bearer_token_authenticates_an_api_call()
        {
            var email = Unique("bearer");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var client = await _factory.CreateBearerClientAsync(email);
            var response = await client.GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task The_token_carries_the_roles_the_account_holds()
        {
            // Without role claims [Authorize(Roles = "Staff,Admin")] on ReportsController could
            // never pass for a bearer caller - the endpoint was not merely protected, it was
            // unreachable.
            var email = Unique("staff");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member, roles: Roles.Staff);

            var client = await _factory.CreateBearerClientAsync(email);
            var response = await client.GetAsync("/api/reports/members");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task A_member_token_cannot_reach_the_staff_report()
        {
            // The other half of the previous test: the roles have to be real, not decorative.
            var email = Unique("plain");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var client = await _factory.CreateBearerClientAsync(email);
            var response = await client.GetAsync("/api/reports/members");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task An_anonymous_api_call_is_refused_with_json_not_a_redirect()
        {
            // A 302 to an HTML login form is unreadable to a JSON client. The old behaviour
            // was exactly that, and the existing coverage only asserted "not 200", which a
            // redirect satisfies.
            var response = await _factory.CreateNonRedirectingClient().GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Contains("message", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_browser_is_still_redirected_to_the_login_page()
        {
            // The guard on the change above. Making bearer the default scheme rather than a
            // forwarding target would turn every Razor redirect into a 401 and break the whole
            // signed-out browsing experience.
            var response = await _factory.CreateNonRedirectingClient().GetAsync("/Members/MemberInfo");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Api_login_no_longer_starts_a_cookie_session()
        {
            // PasswordSignInAsync issued a cookie as a side effect, so the endpoint handed back
            // a token and opened a cookie session, then validated the cookie rather than the
            // token it had just minted.
            var email = Unique("nocookie");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var response = await _factory.CreateNonRedirectingClient().PostAsJsonAsync(
                "/api/auth/login", new { email, password = "TestPw1!x" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"),
                "API login set a cookie; it should return a bearer token and nothing else.");
        }

        [Fact]
        public async Task Deactivating_an_account_stops_its_existing_token_working()
        {
            var email = Unique("revoked");
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var token = await _factory.IssueTokenAsync(email);
            Assert.Equal(HttpStatusCode.OK,
                (await _factory.CreateClientWithToken(token).GetAsync("/api/members/current")).StatusCode);

            // What deactivation does, minus the admin UI: move the security stamp.
            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var stored = await userManager.FindByIdAsync(user.Id);
                await userManager.UpdateSecurityStampAsync(stored!);
            });

            var afterRevocation = await _factory.CreateClientWithToken(token).GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.Unauthorized, afterRevocation.StatusCode);
        }

        [Fact]
        public async Task Logging_out_stops_the_token_working()
        {
            var email = Unique("logout");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var token = await _factory.IssueTokenAsync(email);
            var client = _factory.CreateClientWithToken(token);

            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/auth/logout", null)).StatusCode);

            var afterLogout = await _factory.CreateClientWithToken(token).GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        }

        [Fact]
        public async Task Refresh_issues_a_working_token()
        {
            var email = Unique("refresh");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var original = await _factory.IssueTokenAsync(email);
            var refreshed = await RefreshAsync(original);

            Assert.Equal(HttpStatusCode.OK,
                (await _factory.CreateClientWithToken(refreshed).GetAsync("/api/members/current")).StatusCode);
        }

        [Fact]
        public async Task A_revoked_token_cannot_be_refreshed()
        {
            // Otherwise refresh would launder a revoked token into a valid one and the
            // revocation above would be worthless.
            var email = Unique("norefresh");
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);

            var token = await _factory.IssueTokenAsync(email);

            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var stored = await userManager.FindByIdAsync(user.Id);
                await userManager.UpdateSecurityStampAsync(stored!);
            });

            var response = await _factory.CreateNonRedirectingClient()
                .PostAsJsonAsync("/api/auth/refresh", new { token });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task A_token_that_was_not_signed_by_us_is_refused()
        {
            var response = await _factory.CreateClientWithToken("not.a.token")
                .GetAsync("/api/members/current");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<string> RefreshAsync(string token)
        {
            var response = await _factory.CreateNonRedirectingClient()
                .PostAsJsonAsync("/api/auth/refresh", new { token });

            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Refresh returned {(int)response.StatusCode}. Body: {body}");

            return JsonDocument.Parse(body).RootElement.GetProperty("token").GetString()!;
        }
    }
}
