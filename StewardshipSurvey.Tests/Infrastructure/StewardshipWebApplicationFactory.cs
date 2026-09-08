using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Services;

namespace StewardshipSurvey.Tests.Infrastructure
{
    /// <summary>
    /// Boots the real application against a private SQLite database.
    /// <para>
    /// Each factory instance owns one in-memory database, held alive by keeping
    /// <see cref="_connection"/> open — SQLite destroys a <c>:memory:</c> database as soon as
    /// the last connection closes.
    /// </para>
    /// </summary>
    public class StewardshipWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection =
            new SqliteConnection("DataSource=:memory:;Foreign Keys=True");

        /// <summary>
        /// Where the file-drop email sender writes during this run. Per-factory so parallel
        /// test classes cannot read each other's messages.
        /// </summary>
        public string MailDropPath { get; } =
            Path.Combine(Path.GetTempPath(), "stewardship-tests", Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Not "Development". That environment runs AdminSeeder at startup, which hits the
            // database before the pipeline is built and throws a bare Exception on failure.
            // It also turns on the developer exception page, which would hide real database
            // errors behind a 500 page instead of surfacing them.
            builder.UseEnvironment("Testing");

            // No SMTP host, so the app selects FileDropEmailSender and confirmation messages
            // land here where a test can read them.
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Email:FileDropPath"] = MailDropPath,
                    ["Email:Smtp:Host"] = null,

                    // Signs and now also validates the API's bearer tokens - the bearer handler
                    // has no signing key without it and rejects everything. A fixed literal is
                    // fine: it never leaves this process and the database is thrown away when
                    // the run ends.
                    ["Jwt:Key"] = "test-only-signing-key-not-used-anywhere-else-32+"
                }));

            builder.ConfigureServices(services =>
            {
                // Remove BOTH descriptors. Dropping only the options leaves the context
                // registered against SQL Server and EF reports two providers at first resolve.
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();

                _connection.Open();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

                // The purge sweep starts with the host. Left in place it could delete rows out
                // from under a slow test.
                //
                // Removed by implementation type, not with RemoveAll<IHostedService>(). The
                // blanket version happened to remove exactly this one today, but it would
                // silently stop any hosted service added later from running under every
                // integration test - and a background job that never runs in tests fails
                // quietly, which is the worst way to find out.
                var purge = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(DeactivatedUserPurgeService));

                Assert.NotNull(purge); // the registration moved or was renamed
                services.Remove(purge!);

                using var scope = services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Built from the EF model, not the SQL Server migrations - see the skill notes.
                context.Database.EnsureCreated();
            });
        }

        /// <summary>
        /// A client that reports redirects instead of following them, so a test can assert
        /// both the 302 and its Location. The app also calls UseHttpsRedirection, which would
        /// otherwise send every request on a detour.
        /// </summary>
        public HttpClient CreateNonRedirectingClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        /// <summary>Runs <paramref name="action"/> against a fresh scope's services.</summary>
        public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
        {
            using var scope = Services.CreateScope();
            await action(scope.ServiceProvider);
        }

        /// <summary>
        /// Creates a confirmed, signed-in-capable account. Identity is configured with
        /// RequireConfirmedAccount = true, so an unconfirmed user can never sign in.
        /// </summary>
        public async Task<ApplicationUser> CreateUserAsync(
            string email,
            string password = "TestPw1!x",
            MembershipStatus? status = null,
            params string[] roles)
        {
            ApplicationUser created = null!;

            await WithScopeAsync(async provider =>
            {
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
                var context = provider.GetRequiredService<ApplicationDbContext>();

                foreach (var role in Roles.All)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                Assert.True(result.Succeeded,
                    $"Could not create {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

                await userManager.AddToRoleAsync(user, Roles.RegisteredUser);

                foreach (var role in roles)
                {
                    await userManager.AddToRoleAsync(user, role);
                }

                if (status != null)
                {
                    var profile = new MemberInfo
                    {
                        FirstName = "Test",
                        LastName = email.Split('@')[0],
                        Email = email,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        MembershipStatus = status,
                        ApplicationUser = user
                    };

                    context.MemberInfos.Add(profile);
                    await context.SaveChangesAsync();

                    await MembershipStatusRoles.SyncAsync(userManager, user, status);
                }

                created = user;
            });

            return created;
        }

        /// <summary>
        /// Signs in through the real Identity UI so the returned client carries a genuine auth
        /// cookie. API endpoints accept a cookie as well as a bearer token - the policy scheme
        /// in Program.cs picks on the presence of an Authorization header - so this works for
        /// Razor pages and for the API. Use <see cref="CreateBearerClientAsync"/> when the
        /// bearer path is what is under test.
        /// </summary>
        public async Task<HttpClient> CreateSignedInClientAsync(string email, string password = "TestPw1!x")
        {
            var client = CreateNonRedirectingClient();

            var loginPage = await client.GetStringAsync("/Identity/Account/Login");
            var token = AntiforgeryToken.Extract(loginPage);

            var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Input.Email"] = email,
                    ["Input.Password"] = password,
                    ["Input.RememberMe"] = "false",
                    ["__RequestVerificationToken"] = token
                }));

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.Found,
                $"Sign-in for {email} did not redirect; got {(int)response.StatusCode}. " +
                "A 200 usually means the credentials or the antiforgery token were rejected.");

            return client;
        }

        /// <summary>
        /// Signs in through <c>POST /api/auth/login</c> and returns a client carrying the
        /// bearer token it issued - the path the MAUI app uses. The client holds no cookie:
        /// the API login stopped issuing one, and a test that accidentally relied on the
        /// cookie would prove nothing about bearer authentication.
        /// </summary>
        public async Task<HttpClient> CreateBearerClientAsync(string email, string password = "TestPw1!x")
        {
            var token = await IssueTokenAsync(email, password);
            var client = CreateNonRedirectingClient();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        /// <summary>
        /// The raw token from <c>POST /api/auth/login</c>, for tests that need to hold one and
        /// use it later - after a role change or a deactivation, for instance.
        /// </summary>
        public async Task<string> IssueTokenAsync(string email, string password = "TestPw1!x")
        {
            var response = await CreateNonRedirectingClient().PostAsJsonAsync(
                "/api/auth/login", new { email, password });

            var body = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK,
                $"API login for {email} returned {(int)response.StatusCode}. Body: {body}");

            var token = JsonDocument.Parse(body).RootElement.GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token), "API login returned an empty token.");

            return token!;
        }

        /// <summary>
        /// Attaches <paramref name="token"/> to a fresh client. Used to prove a token that was
        /// valid a moment ago has stopped being accepted.
        /// </summary>
        public HttpClient CreateClientWithToken(string token)
        {
            var client = CreateNonRedirectingClient();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();

                try
                {
                    if (Directory.Exists(MailDropPath)) Directory.Delete(MailDropPath, recursive: true);
                }
                catch (IOException)
                {
                    // A leftover temp directory is not worth failing a test run over.
                }
            }

            base.Dispose(disposing);
        }
    }
}
