using System.Net;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// What a visitor who is not signed in may reach: the home page and the church link,
    /// nothing else.
    /// </summary>
    public class AnonymousAccessTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public AnonymousAccessTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task The_home_page_does_not_leak_its_commented_placeholder()
        {
            // Pages/Index.cshtml carries a commented-out "How / When / Why" block kept as a
            // placeholder, plus an explanatory comment above it. Razor comments do not nest,
            // so an explanation that itself mentions @* is exactly the way to break one.
            var html = await _factory.CreateNonRedirectingClient().GetStringAsync("/");

            Assert.DoesNotContain("PLACEHOLDER", html);
            Assert.DoesNotContain("gettingStartedTitle", html);
            Assert.DoesNotContain("Why should I help my church family?", html);

            // Not introBlock: the home page has a live element by that id above the comment.
            Assert.Contains("id=\"introBlock\"", html);
        }

        [Fact]
        public async Task Home_page_is_public()
        {
            var response = await _factory.CreateNonRedirectingClient().GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Home_page_invites_an_anonymous_visitor_to_begin()
        {
            var html = await _factory.CreateNonRedirectingClient().GetStringAsync("/");

            Assert.Contains("Begin Survey", html);
            Assert.DoesNotContain("Review My Survey", html);
        }

        [Fact]
        public async Task Home_page_links_to_the_church_site()
        {
            var html = await _factory.CreateNonRedirectingClient().GetStringAsync("/");

            Assert.Contains("stmark-wels.org", html);
        }

        [Fact]
        public async Task Nav_offers_no_signed_in_areas()
        {
            var html = await _factory.CreateNonRedirectingClient().GetStringAsync("/");

            Assert.DoesNotContain("My Survey", html);
            Assert.DoesNotContain("Admin Areas", html);
            Assert.DoesNotContain("Member Report", html);
        }

        [Theory]
        [InlineData("/Members/MemberInfo")]
        [InlineData("/Members/SelectInterests")]
        [InlineData("/Members/SelectMemberInvolvement")]
        [InlineData("/Members/SelectMemberServiceRoles")]
        [InlineData("/Admin/Users")]
        [InlineData("/Staff/MemberReport")]
        public async Task Protected_pages_redirect_to_login(string path)
        {
            var response = await _factory.CreateNonRedirectingClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Renamed_admin_routes_no_longer_exist()
        {
            // The pages moved to /Admin/Users and /Admin/EditUser. An old link must 404 rather
            // than quietly resolving.
            var client = _factory.CreateNonRedirectingClient();

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/UserRoles")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/EditUserRoles")).StatusCode);
        }
    }
}
