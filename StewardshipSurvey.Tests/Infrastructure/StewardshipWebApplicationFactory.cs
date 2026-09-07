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

                    // AuthController refuses to mint a token without this and returns 500,
                    // so a successful API sign-in cannot be exercised unless it is set.
                    // A fixed literal is fine here: nothing validates these tokens yet, and
                    // the database is thrown away when the run ends.
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
                services.RemoveAll<IHostedService>();

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
        /// cookie. API endpoints authenticate by cookie too - no JWT bearer scheme is
        /// registered in Program.cs - so this is the only way in.
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
