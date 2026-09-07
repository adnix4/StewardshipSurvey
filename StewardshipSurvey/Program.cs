using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using StewardshipSurvey.Services;

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

// Add HttpClientFactory with proper configuration for authenticated requests
builder.Services.AddHttpClient("ApiClient")
    .ConfigureHttpClient((provider, client) =>
    {
        var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
        var request = httpContextAccessor?.HttpContext?.Request;
        
        if (request != null)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            client.BaseAddress = new Uri(baseUrl);
        }
    });

// Register HttpClient for dependency injection (fallback for direct injection)
builder.Services.AddScoped<HttpClient>(provider =>
{
    var httpClient = new HttpClient();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor?.HttpContext?.Request;
    
    if (request != null)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        httpClient.BaseAddress = new Uri(baseUrl);
    }
    
    return httpClient;
});

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
