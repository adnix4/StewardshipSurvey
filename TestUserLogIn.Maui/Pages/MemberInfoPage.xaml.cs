namespace TestUserLogIn.Maui;

public partial class MemberInfoPage : ContentPage
{
    private readonly MemberInfoViewModel _viewModel;

    public MemberInfoPage(MemberInfoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMemberInfoCommand.ExecuteAsync(null);
    }
}
