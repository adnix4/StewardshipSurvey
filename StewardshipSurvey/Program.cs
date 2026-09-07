using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

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

// Seed roles and development accounts. Development only - a real deployment
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
