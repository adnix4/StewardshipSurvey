namespace TestUserLogIn.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        
        // API Service
        builder.Services
            .AddHttpClient<MemberApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7295"); // Update with your server URL
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1000)));

        // Authentication Service
        builder.Services.AddSingleton<AuthenticationService>();

        // ViewModels
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<MemberInfoViewModel>();
        builder.Services.AddSingleton<SelectInterestsViewModel>();
        builder.Services.AddSingleton<SelectInvolvementsViewModel>();
        builder.Services.AddSingleton<SelectServiceRolesViewModel>();

        // Pages
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<MemberInfoPage>();
        builder.Services.AddSingleton<SelectInterestsPage>();
        builder.Services.AddSingleton<SelectInvolvementsPage>();
        builder.Services.AddSingleton<SelectServiceRolesPage>();

        return builder.Build();
    }
}
