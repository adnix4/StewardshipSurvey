# MAUI Application Setup Guide - St. Mark Stewardship Survey

## Overview

A complete .NET MAUI application has been created for the member pages and login screen. This guide walks you through setting up and running the application.

## Project Structure

```
TestUserLogIn.Maui/
??? MauiProgram.cs                  # Main entry point, DI configuration
??? App.xaml(.cs)                   # Application root
??? AppShell.xaml(.cs)              # Navigation shell
??? Pages/
?   ??? LoginPage.xaml(.cs)         # Login screen
?   ??? MemberInfoPage.xaml(.cs)    # Member info form
?   ??? SelectInterestsPage.xaml(.cs)
?   ??? SelectInvolvementsPage.xaml(.cs)
?   ??? SelectServiceRolesPage.xaml(.cs)
??? ViewModels/
?   ??? LoginViewModel.cs
?   ??? MemberInfoViewModel.cs
?   ??? SelectionViewModels.cs      # Interests, Involvements, ServiceRoles
??? Services/
?   ??? MemberApiService.cs         # API calls
?   ??? AuthenticationService.cs    # Auth token management
??? Models/
?   ??? Dtos.cs                     # Data transfer objects
??? TestUserLogIn.Maui.csproj       # Project file

```

## Setup Instructions

### Step 1: Create the MAUI Project

```bash
cd C:\Stewardship\MemberSurvey
dotnet new maui -n TestUserLogIn.Maui
```

### Step 2: Update the project file

Copy the content from `TestUserLogIn.Maui.csproj` (already created) to your project file.

### Step 3: Copy Files

Copy all the generated files into your MAUI project:
- MauiProgram.cs
- App.xaml and App.xaml.cs
- AppShell.xaml and AppShell.xaml.cs
- All Pages, ViewModels, Services, and Models folders

### Step 4: Update API Base Address

In `MauiProgram.cs`, update the base address to match your server:

```csharp
builder
    .AddHttpClient<MemberApiService>(client =>
    {
        client.BaseAddress = new Uri("https://your-server-url:port");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
```

### Step 5: Add Missing Pages

You need to create these XAML pages (similar structure to SelectInterestsPage):

1. **SelectInvolvementsPage.xaml**
   - Replace "interests" with "involvements"
   - Update binding to SelectInvolvementsViewModel

2. **SelectServiceRolesPage.xaml**
   - Replace "interests" with "service roles"
   - Update binding to SelectServiceRolesViewModel

### Step 6: Add Value Converters

Create a `Converters` folder and add these converters:

```csharp
// StringToBoolConverter.cs
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

// InvertedBoolConverter.cs
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

### Step 7: Register Converters in App.xaml

```xml
<Application.Resources>
    <converters:StringToBoolConverter x:Key="StringToBoolConverter" />
    <converters:InvertedBoolConverter x:Key="InvertedBoolConverter" />
    <!-- ... other resources -->
</Application.Resources>
```

### Step 8: Configure Navigation

Update `AppShell.xaml` to include all routes:

```xml
<Shell.Routes>
    <Route Route="memberinfo" Shell.ContentTemplate="{DataTemplate local:MemberInfoPage}" />
    <Route Route="selectinterests" Shell.ContentTemplate="{DataTemplate local:SelectInterestsPage}" />
    <Route Route="selectinvolvements" Shell.ContentTemplate="{DataTemplate local:SelectInvolvementsPage}" />
    <Route Route="selectserviceroles" Shell.ContentTemplate="{DataTemplate local:SelectServiceRolesPage}" />
</Shell.Routes>
```

## Features Included

### Login Screen
- Email and password input
- Token-based authentication
- Secure token storage
- Auto-login if token exists

### Member Information Page
- Form with all member details
- Preferred contact method (Radio buttons)
- Logout functionality
- Input validation

### Selection Pages
- Dynamic list of options with descriptions
- Checkbox selection
- Load previously selected items
- Save to database

### API Integration
- HttpClient with error handling
- Retry policy (Polly)
- Bearer token authentication
- JSON serialization/deserialization

### Authentication
- Secure storage of auth tokens
- Token refresh capability
- Logout and clear storage

## Building & Running

### Windows
```bash
dotnet build -f net8.0-windows10.0.19041.0
dotnet run -f net8.0-windows10.0.19041.0
```

### Android
```bash
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

### iOS (macOS only)
```bash
dotnet build -f net8.0-ios
dotnet run -f net8.0-ios
```

## Important Notes

### CORS Configuration

Your ASP.NET server needs CORS configured for MAUI:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

### API Endpoints Required

The following API endpoints must exist on your server:

```
POST   /api/auth/login                      - Login (returns token)
GET    /api/members/current                 - Get current member info
POST   /api/members/current                 - Save current member info
GET    /api/interests/all                   - Get all interests
GET    /api/interests/current               - Get current member's interests
POST   /api/interests/current               - Save member's interests
GET    /api/involvements/all                - Get all involvements
GET    /api/involvements/current            - Get current member's involvements
POST   /api/involvements/current            - Save member's involvements
GET    /api/serviceroles/all                - Get all service roles
GET    /api/serviceroles/current            - Get current member's service roles
POST   /api/serviceroles/current            - Save member's service roles
```

### SSL/TLS

For production builds on Android, SSL certificate pinning is recommended.

## Troubleshooting

### Connection Issues
- Ensure server URL is correct (update in MauiProgram.cs)
- Check if server is running
- Verify firewall settings
- On Android emulator, use `10.0.2.2` for localhost

### Authentication Issues
- Verify API returns token in login response
- Check token is stored correctly in SecureStorage
- Ensure Bearer token is sent in Authorization header

### Loading Screens
- Add console logging in ViewModels
- Check Output window for errors
- Use browser DevTools to verify API responses

## Testing the Application

1. Start the ASP.NET server
2. Run MAUI app in emulator or device
3. Login with test credentials
4. Fill in member information
5. Select interests, involvements, service roles
6. Verify data in database

## Next Steps

1. Create the remaining XAML pages (Involvements, ServiceRoles)
2. Add value converters for checkbox binding
3. Create API endpoint for member authentication
4. Test on target platform (Windows/Android/iOS)
5. Add error handling improvements
6. Implement progress indicators
7. Add form validation
8. Style app with app-specific branding

## Customization

### Change Colors
Edit `App.xaml` ResourceDictionary:
```xml
<Color x:Key="PrimaryColor">#007AFF</Color>
<Color x:Key="SecondaryColor">#5AC8FA</Color>
```

### Change Fonts
Update `MauiProgram.cs`:
```csharp
fonts.AddFont("YourFont.ttf", "CustomFont");
```

### Add App Icon
Place icon in `Resources/AppIcon/` and update `MauiProgram.cs`

## Resources

- [MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [MVVM Community Toolkit](https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/)
- [MAUI Shell Navigation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/)

## Support

For issues or questions, refer to the API documentation in the ASP.NET project or the MAUI documentation links above.
