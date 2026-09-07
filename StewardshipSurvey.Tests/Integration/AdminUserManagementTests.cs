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
    /// Deactivation, its guards, and reactivation. Everything here goes through HTTP, so the
    /// guards are exercised the way an attacker would reach them - by posting the handler
    /// directly, not by clicking a button the UI may have hidden.
    /// </summary>
    public class AdminUserManagementTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public AdminUserManagementTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task An_admin_cannot_deactivate_their_own_account()
        {
            var (client, admin) = await SignedInAdminAsync();

            var response = await PostDeactivateAsync(client, admin.Id);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // re-rendered, not redirected
            Assert.Contains("cannot deactivate your own account", html);
            await AssertNotDeactivatedAsync(admin.Id);
        }

        [Fact]
        public async Task Deactivation_strips_elevated_roles_and_remembers_them()
        {
            var (client, _) = await SignedInAdminAsync();
            var target = await _factory.CreateUserAsync(
                $"staffer-{Guid.NewGuid():N}@stmark.local", roles: Roles.Staff);

            var response = await PostDeactivateAsync(client, target.Id);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

                var stored = await context.Users.FirstAsync(u => u.Id == target.Id);
                Assert.NotNull(stored.DeactivatedDate);
                Assert.Equal(Roles.Staff, stored.DeactivatedRoles);

                var roles = await userManager.GetRolesAsync(stored);
                Assert.DoesNotContain(Roles.Staff, roles);
                // The catch-all is never stripped.
                Assert.Contains(Roles.RegisteredUser, roles);
            });
        }

        [Fact]
        public async Task A_deactivated_account_cannot_sign_in()
        {
            var (client, _) = await SignedInAdminAsync();
            var email = $"locked-{Guid.NewGuid():N}@stmark.local";
            var target = await _factory.CreateUserAsync(email);

            await PostDeactivateAsync(client, target.Id);

            var anonymous = _factory.CreateNonRedirectingClient();
            var loginPage = await anonymous.GetStringAsync("/Identity/Account/Login");

            var attempt = await anonymous.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = "TestPw1!x",
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(loginPage)
                }));

            // A lockout redirects too, so the status alone proves nothing - it is the
            // destination that distinguishes "signed in" from "refused".
            var destination = attempt.Headers.Location?.OriginalString ?? string.Empty;
            Assert.Equal(HttpStatusCode.Found, attempt.StatusCode);
            Assert.Contains("Lockout", destination);
        }

        [Fact]
        public async Task Reactivating_with_restore_returns_the_remembered_roles()
        {
            var (client, _) = await SignedInAdminAsync();
            var target = await _factory.CreateUserAsync(
                $"restore-{Guid.NewGuid():N}@stmark.local", roles: Roles.Staff);
            await PostDeactivateAsync(client, target.Id);

            await PostReactivateAsync(client, target.Id, restoreRoles: true);

            await AssertRolesAsync(target.Id, shouldContain: Roles.Staff);
            await AssertNotDeactivatedAsync(target.Id);
        }

        [Fact]
        public async Task Reactivating_without_restore_leaves_the_elevated_roles_off()
        {
            var (client, _) = await SignedInAdminAsync();
            var target = await _factory.CreateUserAsync(
                $"norestore-{Guid.NewGuid():N}@stmark.local", roles: Roles.Staff);
            await PostDeactivateAsync(client, target.Id);

            await PostReactivateAsync(client, target.Id, restoreRoles: false);

            await AssertRolesAsync(target.Id, shouldNotContain: Roles.Staff);
            await AssertNotDeactivatedAsync(target.Id);
        }

        [Fact]
        public async Task A_non_admin_cannot_reach_the_admin_pages()
        {
            var email = $"plain-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            var response = await client.GetAsync("/Admin/Users");

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<(HttpClient Client, ApplicationUser Admin)> SignedInAdminAsync()
        {
            var email = $"admin-{Guid.NewGuid():N}@stmark.local";
            var admin = await _factory.CreateUserAsync(email, roles: Roles.Admin);
            var client = await _factory.CreateSignedInClientAsync(email);
            return (client, admin);
        }

        private async Task<HttpResponseMessage> PostDeactivateAsync(HttpClient client, string id)
        {
            var page = await client.GetStringAsync($"/Admin/EditUser?id={id}");

            return await client.PostAsync($"/Admin/EditUser?id={id}&handler=Deactivate",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(page)
                }));
        }

        private async Task PostReactivateAsync(HttpClient client, string id, bool restoreRoles)
        {
            var page = await client.GetStringAsync("/Admin/Users");

            var response = await client.PostAsync("/Admin/Users?handler=Reactivate",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["restoreRoles"] = restoreRoles ? "true" : "false",
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(page)
                }));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        }

        private Task AssertNotDeactivatedAsync(string userId) =>
            _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var user = await context.Users.FirstAsync(u => u.Id == userId);
                Assert.Null(user.DeactivatedDate);
            });

        private Task AssertRolesAsync(string userId, string? shouldContain = null, string? shouldNotContain = null) =>
            _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                var roles = await userManager.GetRolesAsync(user!);

                if (shouldContain != null) Assert.Contains(shouldContain, roles);
                if (shouldNotContain != null) Assert.DoesNotContain(shouldNotContain, roles);
            });
    }
}
