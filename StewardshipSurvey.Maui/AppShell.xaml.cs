namespace StewardshipSurvey.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Every Shell.Current.GoToAsync target has to be registered or navigation throws at
        // runtime. None of them were, so every step of the survey would have failed the moment
        // it was reached.
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("memberinfo", typeof(MemberInfoPage));
        Routing.RegisterRoute("selectinterests", typeof(SelectInterestsPage));
        Routing.RegisterRoute("selectinvolvements", typeof(SelectInvolvementsPage));
        Routing.RegisterRoute("selectserviceroles", typeof(SelectServiceRolesPage));
    }
}
