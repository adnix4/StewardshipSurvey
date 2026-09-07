# St. Mark Stewardship Survey - MAUI Application

A .NET MAUI mobile/desktop application for the St. Mark Stewardship Survey, providing member information management and interest/involvement selection across iOS, Android, macOS, and Windows platforms.

## Features

? **Cross-Platform Support**
- Windows (Desktop)
- Android
- iOS
- macOS

? **Member Features**
- Login with email/password
- Member profile information
- Contact preference selection
- Interest selection
- Involvement area selection
- Service role selection
- Secure token storage

? **Technical Features**
- MVVM architecture with MVVM Community Toolkit
- Async/await patterns
- Dependency injection
- Bearer token authentication
- Secure storage
- Error handling
- Activity indicators

## Project Structure

```
StewardshipSurvey.Maui/
??? MauiProgram.cs                          # Application entry point & DI
??? App.xaml, App.xaml.cs                   # Application resources
??? AppShell.xaml, AppShell.xaml.cs         # Navigation shell
?
??? Pages/
?   ??? LoginPage.xaml(.cs)                 # Login screen
?   ??? MemberInfoPage.xaml(.cs)            # Profile information
?   ??? SelectInterestsPage.xaml(.cs)       # Interest selection
?   ??? SelectInvolvementsPage.xaml(.cs)    # Involvement selection
?   ??? SelectServiceRolesPage.xaml(.cs)    # Service role selection
?
??? ViewModels/
?   ??? LoginViewModel.cs                   # Login logic
?   ??? MemberInfoViewModel.cs              # Profile logic
?   ??? SelectionViewModels.cs              # Interests/Involvements/ServiceRoles logic
?
??? Services/
?   ??? MemberApiService.cs                 # API communication
?   ??? AuthenticationService.cs            # Authentication & token management
?
??? Models/
?   ??? Dtos.cs                             # Data transfer objects
?
??? StewardshipSurvey.Maui.csproj               # Project configuration
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 (or VS Code with MAUI extension)
- Platform-specific requirements:
  - **Windows**: Windows 10 19041 or later
  - **Android**: Android 5.0 (API 21) or later, Android SDK
  - **iOS**: iOS 14.2 or later, macOS with Xcode
  - **macOS**: macOS 10.15 or later

### Installation

1. **Clone the repository**
   ```bash
   cd C:\Stewardship\MemberSurvey
   ```

2. **Create the MAUI project** (if not already created)
   ```bash
   dotnet new maui -n StewardshipSurvey.Maui
   ```

3. **Copy project files**
   - Copy all files from the generated project structure

4. **Restore dependencies**
   ```bash
   cd StewardshipSurvey.Maui
   dotnet restore
   ```

5. **Update Configuration**
   - Edit `MauiProgram.cs`
   - Update the API base address to match your server:
   ```csharp
   client.BaseAddress = new Uri("https://localhost:7295");
   ```

### Running the Application

**Windows (Desktop)**
```bash
dotnet run -f net8.0-windows10.0.19041.0
```

**Android (Emulator)**
```bash
dotnet run -f net8.0-android
```

**iOS (Simulator, macOS only)**
```bash
dotnet run -f net8.0-ios
```

**macOS (Native)**
```bash
dotnet run -f net8.0-maccatalyst
```

## API Endpoints

The application communicates with the following API endpoints:

### Authentication
- `POST /api/auth/login` - Login with email/password (returns JWT token)
- `POST /api/auth/logout` - Logout and invalidate token

### Member Information
- `GET /api/members/current` - Get current user's member information
- `POST /api/members/current` - Save current user's member information
- `GET /api/members/report` - Get all members report (Admin only)

### Interests
- `GET /api/interests/all` - Get all available interests
- `GET /api/interests/current` - Get current user's selected interests
- `POST /api/interests/current` - Save current user's interest selections

### Involvements
- `GET /api/involvements/all` - Get all available involvement areas
- `GET /api/involvements/current` - Get current user's selected involvements
- `POST /api/involvements/current` - Save current user's involvement selections

### Service Roles
- `GET /api/serviceroles/all` - Get all available service roles
- `GET /api/serviceroles/current` - Get current user's selected service roles
- `POST /api/serviceroles/current` - Save current user's service role selections

## Authentication Flow

1. **Login**
   - User enters email and password
   - Credentials sent to `/api/auth/login`
   - Server returns JWT token
   - Token stored securely in device storage

2. **Authenticated Requests**
   - Token included in `Authorization: Bearer {token}` header
   - API validates token and processes request
   - Token remains valid for 7 days

3. **Logout**
   - Token removed from secure storage
   - User redirected to login page

## Data Models

### MemberDto
```csharp
public class MemberDto
{
    public int MemberID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string CellPhoneNumber { get; set; }
    public string HomePhoneNumber { get; set; }
    public string WorkPhoneNumber { get; set; }
    public string PreferredContactEmail { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Sex { get; set; }
    public string Comments { get; set; }
    public bool IsActive { get; set; }
    public bool PrefersPhone { get; set; }
    public bool PrefersEmail { get; set; }
    public bool PrefersText { get; set; }
}
```

### InterestAreaDto / InvolvementAreaDto
```csharp
public class InterestAreaDto
{
    public int InterestAreaID { get; set; }
    public string InterestArea { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}
```

## Services

### MemberApiService
Handles all API communication with the server:
- Gets all available items (interests, involvements, service roles)
- Gets user's current selections
- Saves user's selections
- Gets/saves member information

### AuthenticationService
Manages authentication and token storage:
- Login with email/password
- Secure token storage using `SecureStorage`
- Token restoration on app startup
- Logout and clear stored credentials

## ViewModels

All ViewModels use MVVM Community Toolkit with:
- `ObservableObject` for property change notifications
- `RelayCommand` for button commands
- `ObservableCollection` for lists
- Async command execution with loading states

### LoginViewModel
- Email and password binding
- Login command with error handling
- Auto-login on startup if token exists
- Loading state management

### MemberInfoViewModel
- Member data loading and saving
- Preferred contact method handling
- Form validation
- Navigation to next step
- Logout functionality

### SelectInterestsViewModel
- Load all interests
- Load user's current interests
- Toggle interest selection
- Save selections

### SelectInvolvementsViewModel
- Load all involvements
- Load user's current involvements
- Toggle involvement selection
- Save selections

### SelectServiceRolesViewModel
- Load all service roles
- Load user's current selections
- Toggle role selection
- Save selections and complete flow

## Customization

### Change API Base Address
Edit `MauiProgram.cs`:
```csharp
builder.Services
    .AddHttpClient<MemberApiService>(client =>
    {
        client.BaseAddress = new Uri("https://your-server:port");
    })
```

### Change Colors
Edit `App.xaml` ResourceDictionary:
```xml
<Color x:Key="PrimaryColor">#007AFF</Color>
<Color x:Key="ErrorColor">#FF3B30</Color>
<Color x:Key="SuccessColor">#34C759</Color>
```

### Change Fonts
Add fonts to `Resources/Fonts/` and register in `MauiProgram.cs`:
```csharp
builder
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("YourFont.ttf", "CustomFont");
    })
```

## Debugging

### Enable Logging
Add console logging to viewmodels:
```csharp
Debug.WriteLine($"Loading data: {error}");
```

### Check HTTP Requests
Use Fiddler or Charles to monitor API calls:
- Verify token is in Authorization header
- Check response status codes
- View request/response bodies

### Platform-Specific Issues

**Android Emulator**
- Use `10.0.2.2` instead of `localhost` for API calls
- Update in `MauiProgram.cs`:
```csharp
#if __ANDROID__
    client.BaseAddress = new Uri("https://10.0.2.2:7295");
#else
    client.BaseAddress = new Uri("https://localhost:7295");
#endif
```

**iOS Simulator**
- Simulator uses host machine's network
- `localhost:port` should work normally

**Windows Desktop**
- Uses system network
- `localhost:port` should work normally

## Building for Production

### Android
```bash
dotnet publish -f net8.0-android -c Release
```

### iOS
```bash
dotnet publish -f net8.0-ios -c Release
```

### Windows
```bash
dotnet publish -f net8.0-windows10.0.19041.0 -c Release
```

## Security Considerations

? **Token Storage**
- Tokens stored in secure device storage
- Not logged or exposed
- Cleared on logout

? **HTTPS**
- Always use HTTPS in production
- Implement certificate pinning for Android

? **Input Validation**
- Validate email format
- Validate password requirements
- Sanitize form inputs

? **Error Messages**
- Generic error messages to users
- Detailed logging for developers

## Troubleshooting

### Common Issues

**"Unable to connect to API"**
- Verify server is running
- Check API base address in `MauiProgram.cs`
- Ensure firewall allows connections
- On Android emulator, use `10.0.2.2`

**"Login failed"**
- Verify credentials are correct
- Check API returns token in response
- Ensure `AuthController.cs` is implemented

**"Unauthorized" errors**
- Check token is stored and retrieved
- Verify Bearer token format in header
- Check token hasn't expired

**"UI not updating"**
- Ensure ViewModels inherit from `ObservableObject`
- Use `[ObservableProperty]` attribute
- Call `OnPropertyChanged()` for list updates

## Support & Documentation

- [MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [MVVM Toolkit Guide](https://learn.microsoft.com/en-us/windows/communitytoolkit/mvvm/)
- [Shell Navigation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/)
- [MAUI Security](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)

## Version Info

- **.NET**: 8.0
- **MAUI**: 8.0.80
- **MVVM Toolkit**: 8.2.2
- **Polly**: 8.4.1

## License

Same as parent project

## Authors

St. Mark Development Team

---

**Next Steps:**
1. Implement remaining XAML pages (Involvements, ServiceRoles)
2. Add value converters for checkbox binding
3. Test on target platform
4. Add app icon and splash screen
5. Implement app versioning
6. Set up app store publishing
