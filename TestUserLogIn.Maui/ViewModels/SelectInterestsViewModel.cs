using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TestUserLogIn.Maui.Models;
using TestUserLogIn.Maui.Services;

namespace TestUserLogIn.Maui.ViewModels;

public partial class SelectInterestsViewModel : ObservableObject
{
    private readonly MemberApiService _apiService;
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    ObservableCollection<InterestAreaDto> allInterests = new();

    [ObservableProperty]
    List<int> selectedInterestIds = new();

    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string errorMessage = string.Empty;

    public SelectInterestsViewModel(MemberApiService apiService, AuthenticationService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadInterests()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // Load all interests
            var interests = await _apiService.GetAllInterestsAsync();
            AllInterests = new ObservableCollection<InterestAreaDto>(interests);

            // Load current user's interests
            var token = _authService.GetToken();
            var currentInterests = await _apiService.GetCurrentInterestsAsync(token);
            SelectedInterestIds = currentInterests.Select(i => i.InterestAreaID).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading interests: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ToggleInterest(int interestId)
    {
        if (SelectedInterestIds.Contains(interestId))
        {
            SelectedInterestIds.Remove(interestId);
        }
        else
        {
            SelectedInterestIds.Add(interestId);
        }

        // Notify the UI of the change
        OnPropertyChanged(nameof(SelectedInterestIds));
    }

    [RelayCommand]
    public async Task SaveInterests()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var token = _authService.GetToken();
            var success = await _apiService.SaveInterestsAsync(SelectedInterestIds, token);

            if (success)
            {
                await Shell.Current.GoToAsync("selectinvolvements");
            }
            else
            {
                ErrorMessage = "Failed to save interests";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving interests: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsInterestSelected(int interestId) => SelectedInterestIds.Contains(interestId);
}
