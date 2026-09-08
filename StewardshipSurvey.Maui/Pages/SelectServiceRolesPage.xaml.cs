using StewardshipSurvey.Maui.Models;
using StewardshipSurvey.Maui.ViewModels;

namespace StewardshipSurvey.Maui;

public partial class SelectServiceRolesPage : ContentPage
{
    private readonly SelectServiceRolesViewModel _viewModel;

    public SelectServiceRolesPage(SelectServiceRolesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadServiceRolesCommand.ExecuteAsync(null);

        // Re-check the rows the member has already chosen. The list is loaded in OnAppearing,
        // so this has to run after it or there would be nothing to select.
        ServiceRoleList.SelectedItems = _viewModel.AllServiceRoles
            .Where(a => _viewModel.SelectedServiceRoleIds.Contains(a.InvolvementAreaID))
            .Cast<object>()
            .ToList();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedServiceRoleIds = e.CurrentSelection
            .OfType<InvolvementAreaDto>()
            .Select(a => a.InvolvementAreaID)
            .ToList();
    }
}
