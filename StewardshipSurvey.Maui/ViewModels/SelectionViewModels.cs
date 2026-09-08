using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StewardshipSurvey.Maui.Models;
using StewardshipSurvey.Maui.Services;
using System.Collections.ObjectModel;

namespace StewardshipSurvey.Maui.ViewModels;

public partial class SelectInvolvementsViewModel : ObservableObject
{
    private readonly MemberApiService _apiService;
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    ObservableCollection<InvolvementAreaDto> allInvolvements = new();

    [ObservableProperty]
    List<int> selectedInvolvementIds = new();

    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string errorMessage = string.Empty;

    public SelectInvolvementsViewModel(MemberApiService apiService, AuthenticationService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadInvolvements()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var involvements = await _apiService.GetAllInvolvementsAsync();
            AllInvolvements = new ObservableCollection<InvolvementAreaDto>(involvements);

            var token = _authService.GetToken();
            var currentInvolvements = await _apiService.GetCurrentInvolvementsAsync(token);
            SelectedInvolvementIds = currentInvolvements.Select(i => i.InvolvementAreaID).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading involvements: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ToggleInvolvement(int involvementId)
    {
        if (SelectedInvolvementIds.Contains(involvementId))
        {
            SelectedInvolvementIds.Remove(involvementId);
        }
        else
        {
            SelectedInvolvementIds.Add(involvementId);
        }

        OnPropertyChanged(nameof(SelectedInvolvementIds));
    }

    [RelayCommand]
    public async Task SaveInvolvements()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var token = _authService.GetToken();
            var success = await _apiService.SaveInvolvementsAsync(SelectedInvolvementIds, token);

            if (success)
            {
                await Shell.Current.GoToAsync("selectserviceroles");
            }
            else
            {
                ErrorMessage = "Failed to save involvements";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving involvements: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsInvolvementSelected(int involvementId) => SelectedInvolvementIds.Contains(involvementId);
}

public partial class SelectServiceRolesViewModel : ObservableObject
{
    private readonly MemberApiService _apiService;
    private readonly AuthenticationService _authService;

    [ObservableProperty]
    ObservableCollection<InvolvementAreaDto> allServiceRoles = new();

    [ObservableProperty]
    List<int> selectedServiceRoleIds = new();

    [ObservableProperty]
    bool isLoading = false;

    [ObservableProperty]
    string errorMessage = string.Empty;

    public SelectServiceRolesViewModel(MemberApiService apiService, AuthenticationService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoadServiceRoles()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var serviceRoles = await _apiService.GetAllServiceRolesAsync();
            AllServiceRoles = new ObservableCollection<InvolvementAreaDto>(serviceRoles);

            var token = _authService.GetToken();
            var currentServiceRoles = await _apiService.GetCurrentServiceRolesAsync(token);
            SelectedServiceRoleIds = currentServiceRoles.Select(i => i.InvolvementAreaID).ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading service roles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ToggleServiceRole(int serviceRoleId)
    {
        if (SelectedServiceRoleIds.Contains(serviceRoleId))
        {
            SelectedServiceRoleIds.Remove(serviceRoleId);
        }
        else
        {
            SelectedServiceRoleIds.Add(serviceRoleId);
        }

        OnPropertyChanged(nameof(SelectedServiceRoleIds));
    }

    [RelayCommand]
    public async Task SaveServiceRoles()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var token = _authService.GetToken();
            var success = await _apiService.SaveServiceRolesAsync(SelectedServiceRoleIds, token);

            if (success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Success", "All information saved successfully!", "OK");
                });
                await Shell.Current.GoToAsync("memberinfo");
            }
            else
            {
                ErrorMessage = "Failed to save service roles";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving service roles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsServiceRoleSelected(int serviceRoleId) => SelectedServiceRoleIds.Contains(serviceRoleId);
}
