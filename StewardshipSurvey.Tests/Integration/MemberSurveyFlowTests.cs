using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Membership status decides whether the "areas you are currently serving in" step
    /// applies. The nav hides it for a prospective member; these tests cover the server-side
    /// guard, which is what actually enforces it.
    /// </summary>
    public class MemberSurveyFlowTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public MemberSurveyFlowTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task A_member_may_open_the_currently_serving_step()
        {
            var email = $"member-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            var response = await client.GetAsync("/Members/SelectMemberServiceRoles");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task A_prospective_member_is_redirected_away_from_the_currently_serving_step()
        {
            var email = $"prospect-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.ProspectiveMember);
            var client = await _factory.CreateSignedInClientAsync(email);

            var response = await client.GetAsync("/Members/SelectMemberServiceRoles");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Contains("/Members/MemberInfo", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task The_guard_reads_the_profile_not_the_cached_role_claim()
        {
            // The role mirrors the profile, but role claims live in the auth cookie. If the
            // guard trusted the cookie, a status changed after sign-in would not take effect.
            var email = $"switcher-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            Assert.Equal(HttpStatusCode.OK,
                (await client.GetAsync("/Members/SelectMemberServiceRoles")).StatusCode);

            // Change only the database column, leaving the signed-in cookie untouched.
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var profile = await context.MemberInfos
                    .FirstAsync(m => m.ApplicationUser!.Id == user.Id);
                profile.MembershipStatus = MembershipStatus.ProspectiveMember;
                await context.SaveChangesAsync();
            });

            var afterChange = await client.GetAsync("/Members/SelectMemberServiceRoles");

            Assert.Equal(HttpStatusCode.Found, afterChange.StatusCode);
        }

        [Fact]
        public async Task A_signed_in_member_sees_the_survey_nav_and_the_review_wording()
        {
            var email = $"navcheck-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateSignedInClientAsync(email);

            var html = await client.GetStringAsync("/");

            Assert.Contains("My Survey", html);
            Assert.Contains("Review My Survey", html);
            Assert.DoesNotContain("Begin Survey", html);
            Assert.DoesNotContain("Admin Areas", html);
        }

        [Fact]
        public async Task A_prospective_member_is_not_offered_the_service_roles_link()
        {
            var email = $"navprospect-{Guid.NewGuid():N}@stmark.local";
            await _factory.CreateUserAsync(email, status: MembershipStatus.ProspectiveMember);
            var client = await _factory.CreateSignedInClientAsync(email);

            var html = await client.GetStringAsync("/");

            Assert.Contains("My Survey", html);
            Assert.DoesNotContain("SelectMemberServiceRoles", html);
        }
    }
}
