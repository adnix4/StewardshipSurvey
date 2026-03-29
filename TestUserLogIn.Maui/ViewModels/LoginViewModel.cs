using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TestUserLogIn.Maui.Services;

namespace TestUserLogIn.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    string email = string.Empty;

    [ObservableProperty]
    string password = string.Empty;

    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string errorMessage = string.Empty;

    [ObservableProperty]
    bool isLoginVisible = true;

    public LoginViewModel(AuthenticationService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter email and password";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var success = await _authService.LoginAsync(Email, Password);
            
            if (success)
            {
                // Navigate to member info page
                await Shell.Current.GoToAsync("memberinfo");
            }
            else
            {
                ErrorMessage = "Login failed. Please check your credentials.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CheckExistingLogin()
    {
        try
        {
            var isAuthenticated = await _authService.RestoreTokenAsync();
            if (isAuthenticated)
            {
                // User already logged in, go to member info
                IsLoginVisible = false;
                await Shell.Current.GoToAsync("memberinfo");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking existing login: {ex.Message}");
        }
    }
}
