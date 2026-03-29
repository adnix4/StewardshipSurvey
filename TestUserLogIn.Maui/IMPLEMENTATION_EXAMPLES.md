# MAUI Implementation Examples

## Complete Page Implementation Example

### SelectInvolvementsPage.xaml (Template)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TestUserLogIn.Maui.SelectInvolvementsPage"
             Title="Select Involvement Areas"
             BackgroundColor="{StaticResource PageBackgroundColor}">

    <Grid RowDefinitions="*,Auto" Padding="20">
        <VerticalStackLayout Spacing="15">
            <Label Text="Select the areas of involvement that interest you" 
                   FontSize="18" FontAttributes="Bold" />

            <CollectionView ItemsSource="{Binding AllInvolvements}">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <StackLayout Padding="10" Spacing="5" BorderStroke="{StaticResource PrimaryColor}" 
                                     BorderStrokeThickness="1" Margin="0,5">
                            <Grid ColumnDefinitions="Auto,*">
                                <CheckBox Grid.Column="0" 
                                          IsChecked="{Binding ., Converter={StaticResource InvolvementSelectedConverter}}" />
                                <StackLayout Grid.Column="1" Spacing="2" Margin="10,0,0,0">
                                    <Label Text="{Binding AreaOfInvolvement}" FontAttributes="Bold" />
                                    <Label Text="{Binding Description}" FontSize="12" 
                                           TextColor="{StaticResource TextSecondaryColor}" />
                                </StackLayout>
                            </Grid>
                        </StackLayout>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <Label Text="{Binding ErrorMessage}" TextColor="{StaticResource ErrorColor}"
                   IsVisible="{Binding ErrorMessage, Converter={StaticResource StringToBoolConverter}}"
                   Margin="0,10" />
        </VerticalStackLayout>

        <Button Grid.Row="1" Text="Next" 
                Command="{Binding SaveInvolvementsCommand}"
                IsEnabled="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}" />

        <ActivityIndicator Grid.Row="0" Grid.RowSpan="2" 
                          IsRunning="{Binding IsLoading}"
                          IsVisible="{Binding IsLoading}" />
    </Grid>
</ContentPage>
```

### SelectInvolvementsPage.xaml.cs

```csharp
namespace TestUserLogIn.Maui;

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
    }
}
```

## Value Converter Examples

### StringToBoolConverter.cs

```csharp
using System.Globalization;

namespace TestUserLogIn.Maui.Converters;

public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
```

### InvertedBoolConverter.cs

```csharp
using System.Globalization;

namespace TestUserLogIn.Maui.Converters;

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value is bool && (bool)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value is bool && (bool)value);
    }
}
```

## Register Converters in App.xaml

```xml
<?xml version = "1.0" encoding = "utf-8" ?>
<Application
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:converters="clr-namespace:TestUserLogIn.Maui.Converters"
    x:Class="TestUserLogIn.Maui.App">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Converters -->
            <converters:StringToBoolConverter x:Key="StringToBoolConverter" />
            <converters:InvertedBoolConverter x:Key="InvertedBoolConverter" />
            
            <!-- Colors -->
            <Color x:Key="PageBackgroundColor">#FFFFFF</Color>
            <Color x:Key="PrimaryColor">#007AFF</Color>
            <Color x:Key="SecondaryColor">#5AC8FA</Color>
            <Color x:Key="TextColor">#000000</Color>
            <Color x:Key="TextSecondaryColor">#666666</Color>
            <Color x:Key="ErrorColor">#FF3B30</Color>
            <Color x:Key="SuccessColor">#34C759</Color>

            <!-- Styles -->
            <Style TargetType="Label">
                <Setter Property="TextColor" Value="{StaticResource TextColor}" />
                <Setter Property="FontFamily" Value="OpenSansRegular" />
            </Style>

            <Style TargetType="Button">
                <Setter Property="Background" Value="{StaticResource PrimaryColor}" />
                <Setter Property="TextColor" Value="#FFFFFF" />
                <Setter Property="FontAttributes" Value="Bold" />
                <Setter Property="Padding" Value="10" />
                <Setter Property="CornerRadius" Value="8" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## Platform-Specific Configuration

### Android API Address (MauiProgram.cs)

```csharp
builder.Services
    .AddHttpClient<MemberApiService>(client =>
    {
        #if __ANDROID__
        // Android emulator uses 10.0.2.2 to access host machine
        client.BaseAddress = new Uri("https://10.0.2.2:7295");
        #else
        client.BaseAddress = new Uri("https://localhost:7295");
        #endif
        
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1000)));
```

## Error Handling Examples

### ViewModel Error Handling

```csharp
[RelayCommand]
public async Task LoadData()
{
    IsLoading = true;
    ErrorMessage = string.Empty;

    try
    {
        var data = await _apiService.GetDataAsync();
        if (data != null)
        {
            Items = new ObservableCollection<ItemDto>(data);
        }
        else
        {
            ErrorMessage = "No data available";
        }
    }
    catch (HttpRequestException ex)
    {
        ErrorMessage = "Connection error. Please check your internet connection.";
        Debug.WriteLine($"HTTP Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        ErrorMessage = "An unexpected error occurred. Please try again.";
        Debug.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
    }
}
```

### API Service Error Handling

```csharp
public async Task<List<ItemDto>> GetItemsAsync()
{
    try
    {
        var response = await _httpClient.GetAsync("/api/items");
        
        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"API Error: {response.StatusCode}");
            return new();
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ItemDto>>(json, _jsonOptions) ?? new();
    }
    catch (HttpRequestException ex)
    {
        Debug.WriteLine($"Network Error: {ex.Message}");
        return new();
    }
    catch (JsonException ex)
    {
        Debug.WriteLine($"Parsing Error: {ex.Message}");
        return new();
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Unexpected Error: {ex.Message}");
        return new();
    }
}
```

## Navigation Examples

### Navigate to Another Page

```csharp
// From ViewModel
await Shell.Current.GoToAsync("memberinfo");

// With parameters
await Shell.Current.GoToAsync($"details?id={itemId}");

// Go back
await Shell.Current.GoToAsync("..");
```

### Update AppShell.xaml for Navigation

```xml
<Shell.Routes>
    <Route Route="memberinfo" Shell.ContentTemplate="{DataTemplate local:MemberInfoPage}" />
    <Route Route="selectinterests" Shell.ContentTemplate="{DataTemplate local:SelectInterestsPage}" />
    <Route Route="selectinvolvements" Shell.ContentTemplate="{DataTemplate local:SelectInvolvementsPage}" />
    <Route Route="selectserviceroles" Shell.ContentTemplate="{DataTemplate local:SelectServiceRolesPage}" />
</Shell.Routes>
```

## Async Command Examples

```csharp
// Simple command
[RelayCommand]
public async Task Submit()
{
    // Command executes automatically
}

// Command with parameter
[RelayCommand]
public async Task SelectItem(int itemId)
{
    // Do something with itemId
}

// Command execution
var canExecute = viewModel.SubmitCommand.CanExecute(null);
await viewModel.SubmitCommand.ExecuteAsync(null);
```

## Data Binding Examples

### Binding to ObservableProperty

```xml
<!-- Two-way binding -->
<Entry Text="{Binding FirstName, Mode=TwoWay}" />

<!-- One-way binding -->
<Label Text="{Binding FullName, Mode=OneWay}" />

<!-- Binding command with parameter -->
<Button Command="{Binding SelectItemCommand}" CommandParameter="{Binding ItemId}" />
```

### Binding to ObservableCollection

```xml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <StackLayout Padding="10">
                <Label Text="{Binding Name}" FontAttributes="Bold" />
                <Label Text="{Binding Description}" FontSize="12" />
            </StackLayout>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

## Testing Examples

### Unit Test ViewModel

```csharp
[TestFixture]
public class LoginViewModelTests
{
    private LoginViewModel _viewModel;
    private Mock<AuthenticationService> _authServiceMock;

    [SetUp]
    public void Setup()
    {
        _authServiceMock = new Mock<AuthenticationService>();
        _viewModel = new LoginViewModel(_authServiceMock.Object);
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        _viewModel.Email = "test@example.com";
        _viewModel.Password = "password123";
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _viewModel.LoginCommand.ExecuteAsync(null);

        // Assert
        Assert.IsEmpty(_viewModel.ErrorMessage);
    }
}
```

## Debugging Tips

### Enable Verbose Logging

```csharp
// In MauiProgram.cs
#if DEBUG
builder
    .UseMauiApp<App>()
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
        logging.AddDebug();
    });
#endif
```

### Debug API Calls

```csharp
// In API Service
Debug.WriteLine($"Request: {request.RequestUri}");
var response = await _httpClient.SendAsync(request);
Debug.WriteLine($"Response: {response.StatusCode}");
var content = await response.Content.ReadAsStringAsync();
Debug.WriteLine($"Body: {content}");
```

### Check Property Changes

```csharp
// In ViewModel
[ObservableProperty]
string email = string.Empty;

// Property changed is automatically logged
// Add your own logging:
partial void OnEmailChanged(string value)
{
    Debug.WriteLine($"Email changed to: {value}");
}
```

---

These examples provide templates and patterns for common MAUI development tasks. Use them as reference when implementing the remaining pages and features.
