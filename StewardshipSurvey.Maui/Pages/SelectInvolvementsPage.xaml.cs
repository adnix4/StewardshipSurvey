using StewardshipSurvey.Maui.Models;
using StewardshipSurvey.Maui.ViewModels;

namespace StewardshipSurvey.Maui;

public partial class SelectInvolvementsPage : ContentPage
{
    private readonly SelectInvolvementsViewModel _viewModel;

    public SelectInvolvementsPage(SelectInvolvementsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInvolvementsCommand.ExecuteAsync(null);

        // Re-check the rows the member has already chosen. The list is loaded in OnAppearing,
        // so this has to run after it or there would be nothing to select.
        InvolvementList.SelectedItems = _viewModel.AllInvolvements
            .Where(a => _viewModel.SelectedInvolvementIds.Contains(a.InvolvementAreaID))
            .Cast<object>()
            .ToList();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedInvolvementIds = e.CurrentSelection
            .OfType<InvolvementAreaDto>()
            .Select(a => a.InvolvementAreaID)
            .ToList();
    }
}
