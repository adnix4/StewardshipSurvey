using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StewardshipSurvey.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using StewardshipSurvey.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        // Stated rather than inherited. These happen to match the framework defaults, but
        // both sign-in paths passed lockoutOnFailure: false until recently, so the policy
        // had no effect at all and nothing on screen said so.
        //
        // Five attempts per fifteen minutes caps a single account at roughly 480 guesses a
        // day. That is not a defence against a leaked hash; it is a defence against someone
        // pointing a list of common passwords at the login form.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// API bearer authentication, added alongside the Identity cookie without disturbing it.
//
// AddDefaultIdentity makes the cookie the default authenticate and challenge scheme, which is
// why an Authorization: Bearer header used to be ignored outright - the MAUI app had been
// sending one to every endpoint and no part of the server ever looked at it.
//
// The defaults are deliberately left alone here. Razor Pages keep challenging via the cookie,
// and every API controller names the bearer scheme on its own [Authorize] - which is also what
// makes the API bearer-*only*: a browser attaches its cookie automatically and [ApiController]
// applies no antiforgery check, so accepting one would put every state-changing endpoint one
// cross-site request away from a member's browser.
//
// Naming the scheme per controller is easy to forget on a new one, and forgetting is silent,
// so Unit/ApiControllerAuthorizationTests enforces it structurally.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<AccessTokenIssuer>();

builder.Services.AddAuthentication().AddJwtBearer();

// Configured through the options system rather than inline, so the signing key is read when
// the handler is first used rather than while the container is still being built. Reading it
// eagerly here looked equivalent and was not: configuration sources added after this line -
// which is how the test host injects its key - had not been applied yet, so the bearer scheme
// silently configured itself with no key and rejected every token it had just issued.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, accessor) =>
    {
        var jwt = accessor.Value;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,

            // No key configured means no token can be honoured. Leaving it null makes every
            // bearer request fail closed, which is the right default: the alternative was a
            // startup crash for any developer without the user secret set.
            IssuerSigningKey = AccessTokenIssuer.IsConfigured(jwt)
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key!))
                : null,
            ValidateLifetime = true,

            // Five minutes is the library default and is generous now that a token lives for
            // an hour rather than a week.
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        bearer.Events = new JwtBearerEvents
        {
                // This is what makes a bearer token revocable. A JWT cannot be recalled once
                // signed, so without this check a deactivated member kept full API access
                // until their token expired. Comparing the stamp baked into the token against
                // the stored one costs a primary-key lookup per authenticated API request -
                // a real, permanent cost, accepted because the alternative is a token that
                // outlives the account it belongs to.
                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<ApplicationUser>>();

                    var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var stamp = context.Principal?.FindFirstValue(StewardshipClaims.SecurityStamp);
                    var user = userId == null ? null : await userManager.FindByIdAsync(userId);

                    if (user == null || stamp == null ||
                        stamp != await userManager.GetSecurityStampAsync(user))
                    {
                        context.Fail("The token has been superseded.");
                    }
                },

                // The handler's default 401 has an empty body. Every other response from this
                // API is a JSON object with a message, and a client that has to special-case
                // one empty response is a client that will get it wrong.
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return context.Response.WriteAsJsonAsync(new { message = "Forbidden" });
                }
        };
    });

// Outgoing mail. AddDefaultIdentity registers NoOpEmailSender with TryAddTransient, which
// silently discarded every confirmation email; registering here afterwards replaces it.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

var smtpHost = builder.Configuration[$"{EmailOptions.SectionName}:Smtp:Host"];
if (string.IsNullOrWhiteSpace(smtpHost))
{
    // No mail server configured: write .eml files to disk rather than pretend to send.
    builder.Services.AddTransient<IEmailSender, FileDropEmailSender>();
}
else
{
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
}

// Retention policy for deactivated accounts, plus the sweep that enforces it.
builder.Services.Configure<UserRetentionOptions>(
    builder.Configuration.GetSection(UserRetentionOptions.SectionName));
builder.Services.AddHostedService<DeactivatedUserPurgeService>();

// Add Controllers for API
builder.Services.AddControllers();

// CORS, off unless origins are configured.
//
// This was AllowAnyOrigin + AllowAnyMethod + AllowAnyHeader on every request. Two things were
// wrong with that. The only API client is the MAUI app, which is native: it sends no Origin
// header, so CORS never applied to it and the policy bought nothing. And it could not have
// served a browser client either, because AllowAnyOrigin cannot be combined with credentials
// and this API authenticates by cookie.
//
// So the honest default is no CORS at all. To add a browser client, list its exact origins
// under Cors:AllowedOrigins.
const string CorsPolicyName = "ConfiguredOrigins";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        // AllowCredentials is what makes the cookie travel, and it is legal here only
        // because the origins are explicit. Listing an origin therefore grants it
        // authenticated access - so list one only if it is as trusted as this app.
        options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
    });
}

// Add IHttpContextAccessor to access current request context
builder.Services.AddHttpContextAccessor();

// Note: two HttpClient registrations used to sit here - a named "ApiClient" from
// AddHttpClient and a raw `new HttpClient()` registered as scoped below it. Neither had a
// single consumer: nothing in this application injects HttpClient or IHttpClientFactory, and
// the MAUI client has its own. The raw one was the listed defect; the named one it pointed to
// as the correct alternative was equally unused, so both are gone rather than one.

var app = builder.Build();

// Roles must exist in every environment. AddToRoleAsync throws on a missing role, so
// registration would fail outright wherever the development seeder does not run.
using (var scope = app.Services.CreateScope())
{
    await AdminSeeder.EnsureRolesAsync(scope.ServiceProvider);
}

// Development accounts and the confirmation backfill. Development only - a real deployment
// provisions its administrator separately, not from application startup.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await AdminSeeder.SeedAsync(services);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// UseCors has to sit after UseRouting and before UseAuthorization. It used to run before
// UseRouting, where no endpoint has been selected yet, so the endpoint's CORS metadata was
// not available and the policy did nothing even where it was meant to apply.
if (allowedOrigins.Length > 0)
{
    app.UseCors(CorsPolicyName);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Map API controllers
app.MapControllers();

app.Run();

// Exposes the implicit Program class to the test project so WebApplicationFactory<Program>
// can boot the real application. Top-level statements otherwise generate it as internal.
public partial class Program { }
