using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Brute-force protection on both sign-in paths.
    /// <para>
    /// Both passed <c>lockoutOnFailure: false</c> - the API by oversight, the Razor page
    /// because that is the scaffolded default - so <c>AccessFailedCount</c> never moved and
    /// the lockout branches in both files were unreachable. These tests exist so that cannot
    /// come back quietly.
    /// </para>
    /// </summary>
    public class LockoutTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public LockoutTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        private async Task<int> FailedCountAsync(string email)
        {
            var count = 0;
            await _factory.WithScopeAsync(async provider =>
            {
                var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await users.FindByEmailAsync(email);
                count = await users.GetAccessFailedCountAsync(user!);
            });
            return count;
        }

        private async Task<HttpResponseMessage> ApiLoginAsync(
            HttpClient client, string email, string password)
        {
            return await client.PostAsJsonAsync("/api/auth/login",
                new { Email = email, Password = password });
        }

        private async Task<HttpResponseMessage> RazorLoginAsync(
            HttpClient client, string email, string password)
        {
            var page = await client.GetStringAsync("/Identity/Account/Login");

            return await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false",
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(page)
                }));
        }

        [Fact]
        public async Task A_failed_api_login_is_counted()
        {
            var email = Unique("api-count");
            await _factory.CreateUserAsync(email);

            await ApiLoginAsync(_factory.CreateNonRedirectingClient(), email, "WrongPassword1!");

            Assert.Equal(1, await FailedCountAsync(email));
        }

        [Fact]
        public async Task The_api_locks_the_account_after_five_failures()
        {
            var email = Unique("api-lock");
            await _factory.CreateUserAsync(email);
            var client = _factory.CreateNonRedirectingClient();

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                await ApiLoginAsync(client, email, "WrongPassword1!");
            }

            var response = await ApiLoginAsync(client, email, "WrongPassword1!");

            Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
        }

        /// <summary>
        /// The part that actually protects anyone: once locked, the real password stops
        /// working too. A lockout that still admitted correct credentials would be decoration.
        /// </summary>
        [Fact]
        public async Task A_locked_account_refuses_even_the_correct_password()
        {
            var email = Unique("api-correct");
            await _factory.CreateUserAsync(email);
            var client = _factory.CreateNonRedirectingClient();

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                await ApiLoginAsync(client, email, "WrongPassword1!");
            }

            var response = await ApiLoginAsync(client, email, "TestPw1!x");

            Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
            Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_failed_razor_login_is_counted()
        {
            var email = Unique("razor-count");
            await _factory.CreateUserAsync(email);

            await RazorLoginAsync(_factory.CreateNonRedirectingClient(), email, "WrongPassword1!");

            Assert.Equal(1, await FailedCountAsync(email));
        }

        /// <summary>
        /// Neither path may be a way around the other. The counter has to be shared, or an
        /// attacker simply picks whichever endpoint is not counting.
        /// </summary>
        [Fact]
        public async Task Failures_on_the_two_paths_share_one_counter()
        {
            var email = Unique("shared-counter");
            await _factory.CreateUserAsync(email);
            var client = _factory.CreateNonRedirectingClient();

            await ApiLoginAsync(client, email, "WrongPassword1!");
            await RazorLoginAsync(client, email, "WrongPassword1!");

            Assert.Equal(2, await FailedCountAsync(email));
        }

        [Fact]
        public async Task A_successful_sign_in_clears_the_counter()
        {
            var email = Unique("reset");
            await _factory.CreateUserAsync(email);
            var client = _factory.CreateNonRedirectingClient();

            await ApiLoginAsync(client, email, "WrongPassword1!");
            Assert.Equal(1, await FailedCountAsync(email));

            var response = await ApiLoginAsync(client, email, "TestPw1!x");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(0, await FailedCountAsync(email));
        }

        /// <summary>
        /// An unknown address must look exactly like a wrong password, or the endpoint
        /// becomes a way to test which addresses have accounts.
        /// </summary>
        [Fact]
        public async Task An_unknown_address_is_indistinguishable_from_a_wrong_password()
        {
            var email = Unique("known");
            await _factory.CreateUserAsync(email);
            var client = _factory.CreateNonRedirectingClient();

            var unknown = await ApiLoginAsync(client, Unique("nobody"), "WrongPassword1!");
            var wrongPassword = await ApiLoginAsync(client, email, "WrongPassword1!");

            Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(
                await unknown.Content.ReadAsStringAsync(),
                await wrongPassword.Content.ReadAsStringAsync());
        }
    }

    /// <summary>
    /// What a member may change about their own profile through the form.
    /// <para>
    /// The page bound the <c>MemberInfo</c> entity itself, so a hand-written POST could carry
    /// server-owned columns alongside the real fields. It now binds
    /// <c>MemberProfileInput</c>, which declares only what the form owns.
    /// </para>
    /// </summary>
    public class ProfileOverpostingTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public ProfileOverpostingTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        private async Task<MemberInfo> ProfileAsync(int memberId)
        {
            MemberInfo found = null!;
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                found = await context.MemberInfos.AsNoTracking()
                    .FirstAsync(m => m.MemberID == memberId);
            });
            return found;
        }

        private static Dictionary<string, string> ValidForm() => new()
        {
            ["MemberDetails.FirstName"] = "Given",
            ["MemberDetails.LastName"] = "Family",
            ["MemberDetails.Email"] = "given.family@stmark.local",
            ["MemberDetails.MembershipStatus"] = nameof(MembershipStatus.Member),
            ["PreferredContact"] = "Email"
        };

        private async Task<HttpResponseMessage> PostProfileAsync(
            HttpClient client, Dictionary<string, string> form)
        {
            var page = await client.GetStringAsync("/Members/MemberInfo");
            form["__RequestVerificationToken"] = AntiforgeryToken.Extract(page);
            return await client.PostAsync("/Members/MemberInfo", new FormUrlEncodedContent(form));
        }

        [Fact]
        public async Task An_ordinary_save_still_works()
        {
            var user = await _factory.CreateUserAsync(Unique("saver"), status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(user.Email!);

            var form = ValidForm();
            form["MemberDetails.Address"] = "8 Ordinary Avenue";

            var response = await PostProfileAsync(client, form);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);

            var saved = await ProfileAsync(user.MemberID!.Value);
            Assert.Equal("Given", saved.FirstName);
            Assert.Equal("8 Ordinary Avenue", saved.Address);
        }

        [Fact]
        public async Task Server_owned_columns_ignore_posted_values()
        {
            var user = await _factory.CreateUserAsync(Unique("overpost"), status: MembershipStatus.Member);
            var before = await ProfileAsync(user.MemberID!.Value);

            var client = await _factory.CreateSignedInClientAsync(user.Email!);

            var form = ValidForm();
            form["MemberDetails.MemberID"] = "9999";
            form["MemberDetails.IsActive"] = "false";
            form["MemberDetails.CreatedDate"] = "1990-01-01";
            form["MemberDetails.UpdatedDate"] = "1990-01-01";

            var response = await PostProfileAsync(client, form);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);

            var after = await ProfileAsync(user.MemberID!.Value);
            Assert.True(after.IsActive, "IsActive was overwritten from the request.");
            Assert.Equal(before.CreatedDate, after.CreatedDate);
            Assert.Equal(before.MemberID, after.MemberID);

            // UpdatedDate is stamped by the page, so a posted value must not survive - it is
            // the field ApplicationUserID used to stand in for here, that column having been
            // removed as a foreign key that never was one.
            Assert.NotEqual(new DateTime(1990, 1, 1), after.UpdatedDate);

            // The form's own fields still had to save, or this would pass on a page that
            // simply ignored the whole POST.
            Assert.Equal("Given", after.FirstName);
        }

        /// <summary>
        /// Deactivation is an administrator's decision. A member must not be able to undo it
        /// by posting IsActive from their own profile form.
        /// </summary>
        [Fact]
        public async Task A_member_cannot_reactivate_their_own_profile()
        {
            var user = await _factory.CreateUserAsync(Unique("deactivated"), status: MembershipStatus.Member);
            var memberId = user.MemberID!.Value;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var profile = await context.MemberInfos.FirstAsync(m => m.MemberID == memberId);
                profile.IsActive = false;
                await context.SaveChangesAsync();
            });

            var client = await _factory.CreateSignedInClientAsync(user.Email!);

            var form = ValidForm();
            form["MemberDetails.IsActive"] = "true";
            await PostProfileAsync(client, form);

            Assert.False((await ProfileAsync(memberId)).IsActive,
                "A member reactivated their own profile by posting IsActive.");
        }
    }
}
