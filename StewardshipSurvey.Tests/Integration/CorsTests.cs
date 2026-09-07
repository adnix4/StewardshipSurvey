using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Which browser origins may call the API.
    /// <para>
    /// The policy used to be <c>AllowAnyOrigin</c> applied unconditionally. It is now off
    /// unless <c>Cors:AllowedOrigins</c> lists something, so both halves are worth pinning:
    /// that the default really is closed, and that configuring an origin really does open it.
    /// </para>
    /// <para>
    /// Note what these tests do not cover. The middleware was also ordered before
    /// <c>UseRouting</c>, which stops endpoint CORS metadata from being applied; with a
    /// single blanket policy and no <c>[EnableCors]</c> attributes anywhere, that misordering
    /// had no observable effect, so no test here would have caught it. It is fixed because
    /// the order is a documented requirement, not because a test demanded it.
    /// </para>
    /// </summary>
    public class CorsTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private const string Header = "Access-Control-Allow-Origin";

        private readonly StewardshipWebApplicationFactory _factory;

        public CorsTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task No_origin_is_allowed_by_default()
        {
            var client = _factory.CreateNonRedirectingClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/current");
            request.Headers.Add("Origin", "https://not-us.example");

            var response = await client.SendAsync(request);

            Assert.False(response.Headers.Contains(Header),
                "The API told a browser that an unlisted origin may read the response.");
        }

        [Fact]
        public async Task A_preflight_from_an_unlisted_origin_is_not_granted()
        {
            var client = _factory.CreateNonRedirectingClient();

            var request = new HttpRequestMessage(HttpMethod.Options, "/api/members/current");
            request.Headers.Add("Origin", "https://not-us.example");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            var response = await client.SendAsync(request);

            Assert.False(response.Headers.Contains(Header));
        }

        /// <summary>
        /// The other half. A closed default is only useful if the switch still works, or the
        /// next person to need a browser client will reach for AllowAnyOrigin again.
        /// </summary>
        [Fact]
        public async Task A_configured_origin_is_allowed()
        {
            using var configured = _factory.WithWebHostBuilder(builder =>
                builder.UseSetting("Cors:AllowedOrigins:0", "https://trusted.example"));

            var client = configured.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/current");
            request.Headers.Add("Origin", "https://trusted.example");

            var response = await client.SendAsync(request);

            Assert.True(response.Headers.Contains(Header),
                "A listed origin was not granted access, so the setting does nothing.");
            Assert.Equal("https://trusted.example",
                response.Headers.GetValues(Header).Single());
        }

        [Fact]
        public async Task Configuring_one_origin_does_not_admit_the_others()
        {
            using var configured = _factory.WithWebHostBuilder(builder =>
                builder.UseSetting("Cors:AllowedOrigins:0", "https://trusted.example"));

            var client = configured.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/current");
            request.Headers.Add("Origin", "https://not-us.example");

            var response = await client.SendAsync(request);

            Assert.False(response.Headers.Contains(Header));
        }
    }
}
