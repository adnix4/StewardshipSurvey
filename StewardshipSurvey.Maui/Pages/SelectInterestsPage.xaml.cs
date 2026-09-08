using StewardshipSurvey.Maui.ViewModels;
namespace StewardshipSurvey.Maui;

public partial class SelectInterestsPage : ContentPage
{
    private readonly SelectInterestsViewModel _viewModel;

    public SelectInterestsPage(SelectInterestsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInterestsCommand.ExecuteAsync(null);
    }
}
