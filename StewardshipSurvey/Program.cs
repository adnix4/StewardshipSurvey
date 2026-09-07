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

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

// Enable CORS
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Map API controllers
app.MapControllers();

app.Run();

// Exposes the implicit Program class to the test project so WebApplicationFactory<Program>
// can boot the real application. Top-level statements otherwise generate it as internal.
public partial class Program { }
