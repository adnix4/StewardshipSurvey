using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using System.Web;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Public registration, end to end and with no mail server. Registration used to call a
    /// no-op sender that discarded the confirmation email, while the framework's confirmation
    /// page handed the registrant a working link - so confirming an address proved nothing.
    /// </summary>
    public class RegistrationTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public RegistrationTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Registering_writes_a_confirmation_email()
        {
            var email = $"reg-{Guid.NewGuid():N}@stmark.local";

            await RegisterAsync(email);

            Assert.NotNull(FindMessageFor(email));
        }

        [Fact]
        public async Task The_confirmation_page_does_not_leak_the_link_outside_development()
        {
            // The factory runs as "Testing". If the link appeared here it would appear in
            // production too, and anyone could confirm their own address.
            var email = $"leak-{Guid.NewGuid():N}@stmark.local";

            var confirmationPage = await RegisterAsync(email);

            Assert.DoesNotContain("ConfirmEmail", confirmationPage);
        }

        [Fact]
        public async Task An_unconfirmed_account_is_told_why_it_cannot_sign_in()
        {
            var email = $"unconfirmed-{Guid.NewGuid():N}@stmark.local";
            await RegisterAsync(email);

            var html = await AttemptSignInAsync(email);

            Assert.Contains("confirm your email address", html);
            Assert.DoesNotContain("Invalid login attempt", html);
        }

        [Fact]
        public async Task Following_the_emailed_link_confirms_the_account_and_allows_sign_in()
        {
            var email = $"confirm-{Guid.NewGuid():N}@stmark.local";
            await RegisterAsync(email);

            var link = ConfirmationLinkFor(email);
            var client = _factory.CreateNonRedirectingClient();

            var confirm = await client.GetAsync(link);
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

            // Now the credentials work, which they did not a moment ago.
            var signedIn = await _factory.CreateSignedInClientAsync(email, "RegPw1!x");
            var home = await signedIn.GetStringAsync("/");
            Assert.Contains("Review My Survey", home);
        }

        [Fact]
        public async Task A_registered_account_gets_the_RegisteredUser_role()
        {
            var email = $"role-{Guid.NewGuid():N}@stmark.local";
            await RegisterAsync(email);

            await _factory.WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByEmailAsync(email);

                Assert.NotNull(user);
                Assert.Contains(Roles.RegisteredUser, await userManager.GetRolesAsync(user!));
            });
        }

        /// <summary>Registers an account and returns the confirmation page's HTML.</summary>
        private async Task<string> RegisterAsync(string email, string password = "RegPw1!x")
        {
            var client = _factory.CreateNonRedirectingClient();
            var page = await client.GetStringAsync("/Identity/Account/Register");

            var response = await client.PostAsync("/Identity/Account/Register",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["Input.ConfirmPassword"] = password,
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(page)
                }));

            if (response.StatusCode != HttpStatusCode.Found)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Register returned {(int)response.StatusCode}. Body: {body}");
            }

            var destination = response.Headers.Location!.OriginalString;
            Assert.Contains("RegisterConfirmation", destination);

            return await client.GetStringAsync(destination);
        }

        private async Task<string> AttemptSignInAsync(string email, string password = "RegPw1!x")
        {
            var client = _factory.CreateNonRedirectingClient();
            var page = await client.GetStringAsync("/Identity/Account/Login");

            var response = await client.PostAsync("/Identity/Account/Login",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["__RequestVerificationToken"] = AntiforgeryToken.Extract(page)
                }));

            return await response.Content.ReadAsStringAsync();
        }

        private string? FindMessageFor(string email) =>
            Directory.Exists(_factory.MailDropPath)
                ? Directory.GetFiles(_factory.MailDropPath, "*.eml")
                    .FirstOrDefault(f => File.ReadAllText(f).Contains(email))
                : null;

        /// <summary>Pulls the confirmation URL out of the message that was written to disk.</summary>
        private string ConfirmationLinkFor(string email)
        {
            var path = FindMessageFor(email);
            Assert.NotNull(path);

            var match = Regex.Match(File.ReadAllText(path!), @"href='(?<url>[^']+)'");
            Assert.True(match.Success, "No confirmation link in the message written to disk.");

            return HttpUtility.HtmlDecode(match.Groups["url"].Value);
        }
    }
}
