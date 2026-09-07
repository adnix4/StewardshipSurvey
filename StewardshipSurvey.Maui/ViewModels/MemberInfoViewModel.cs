using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StewardshipSurvey.Maui.Models;
using StewardshipSurvey.Maui.Services;

namespace StewardshipSurvey.Maui.ViewModels;

public partial class MemberInfoViewModel : ObservableObject
{
    private readonly MemberApiService _apiService;
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    MemberDto memberInfo = new();

    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string errorMessage = string.Empty;

    [ObservableProperty]
    string preferredContact = string.Empty;

    public MemberInfoViewModel(MemberApiService apiService, AuthenticationService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadMemberInfo()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "Authentication required";
                return;
            }

            var member = await _apiService.GetMemberInfoAsync(token);
            if (member != null && member.MemberID > 0)
            {
                MemberInfo = member;

                // Set preferred contact
                if (member.PrefersPhone)
                    PreferredContact = "Phone";
                else if (member.PrefersText)
                    PreferredContact = "Text";
                else if (member.PrefersEmail)
                    PreferredContact = "Email";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading member info: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveMemberInfo()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // Reset preferences
            MemberInfo.PrefersPhone = false;
            MemberInfo.PrefersText = false;
            MemberInfo.PrefersEmail = false;

            // Set selected preference
            switch (PreferredContact)
            {
                case "Phone":
                    MemberInfo.PrefersPhone = true;
                    break;
                case "Text":
                    MemberInfo.PrefersText = true;
                    break;
                case "Email":
                    MemberInfo.PrefersEmail = true;
                    break;
            }

            var token = _authService.GetToken();
            var success = await _apiService.SaveMemberInfoAsync(MemberInfo, token);

            if (success)
            {
                // Navigate to next page (SelectInterests)
                await Shell.Current.GoToAsync("selectinterests");
            }
            else
            {
                ErrorMessage = "Failed to save member information";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving member info: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task Logout()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("login");
    }
}
