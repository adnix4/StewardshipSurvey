using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Saving a profile through the API - <c>PUT /api/members/current</c>.
    /// <para>
    /// The endpoint had no coverage at all: every other test against
    /// <c>/api/members/current</c> used GET. The MAUI client had been sending <c>POST</c> to
    /// this route since it was written, which the server does not expose, so saving a profile
    /// from the app returned 405 and could never have worked. Nothing on either side noticed.
    /// </para>
    /// <para>
    /// The client is fixed; these pin the contract it now depends on, on the side that can
    /// actually be tested.
    /// </para>
    /// </summary>
    public class MemberProfileApiTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public MemberProfileApiTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}@stmark.local";

        [Fact]
        public async Task Put_saves_the_callers_own_profile()
        {
            var email = Unique("save");
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            var response = await client.PutAsJsonAsync("/api/members/current", new
            {
                FirstName = "Given",
                LastName = "Family",
                Email = email,
                CellPhoneNumber = "555 0100",
                Address = "12 Saved Street",
                PrefersEmail = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var saved = await ProfileAsync(user.MemberID!.Value);
            Assert.Equal("Given", saved.FirstName);
            Assert.Equal("12 Saved Street", saved.Address);
            Assert.True(saved.PrefersEmail);
        }

        [Fact]
        public async Task Post_is_not_accepted_on_that_route()
        {
            // The exact shape of the client bug: the route exists, the verb does not. Worth
            // pinning, because a 405 reads like a routing problem rather than a wrong verb.
            var email = Unique("wrongverb");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            var response = await client.PostAsJsonAsync("/api/members/current", new
            {
                FirstName = "Given",
                LastName = "Family"
            });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public async Task Put_round_trips_through_the_get_the_client_reads_back()
        {
            // The client saves and then re-reads. If the two disagree about a field the app
            // would silently lose it.
            var email = Unique("roundtrip");
            await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var client = await _factory.CreateBearerClientAsync(email);

            await client.PutAsJsonAsync("/api/members/current", new
            {
                FirstName = "Round",
                LastName = "Trip",
                Email = email,
                Comments = "kept",
                PrefersText = true
            });

            var body = await client.GetStringAsync("/api/members/current");

            Assert.Contains("Round", body);
            Assert.Contains("kept", body);
        }

        [Fact]
        public async Task Put_refuses_an_anonymous_caller()
        {
            var response = await _factory.CreateNonRedirectingClient()
                .PutAsJsonAsync("/api/members/current", new { FirstName = "Nobody" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Put_cannot_reach_another_members_profile()
        {
            // There is no id on this route, so the only profile reachable is the caller's own.
            // This guards that it stays that way.
            var owner = Unique("owner");
            var ownerUser = await _factory.CreateUserAsync(owner, status: MembershipStatus.Member);

            var other = Unique("other");
            await _factory.CreateUserAsync(other, status: MembershipStatus.Member);
            var otherClient = await _factory.CreateBearerClientAsync(other);

            await otherClient.PutAsJsonAsync("/api/members/current", new
            {
                FirstName = "Trespasser",
                LastName = "Family"
            });

            var ownerProfile = await ProfileAsync(ownerUser.MemberID!.Value);
            Assert.NotEqual("Trespasser", ownerProfile.FirstName);
        }

        private async Task<MemberInfo> ProfileAsync(int memberId)
        {
            MemberInfo profile = null!;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                profile = await context.MemberInfos.AsNoTracking()
                    .FirstAsync(m => m.MemberID == memberId);
            });

            return profile;
        }
    }
}
