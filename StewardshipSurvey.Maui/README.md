# St. Mark Stewardship Survey - MAUI Application

A .NET MAUI client for the St. Mark Stewardship Survey: sign in, keep your profile up to date,
and choose the interests, ministries and service roles you want to be part of. It talks to the
`/api` surface of the `StewardshipSurvey` web application.

## Status - read this first

This project is further from finished than the rest of the repository, and this file used to
describe what it was meant to be rather than what it is. What is actually true today:

- **It builds, for Windows only.** `net8.0-windows10.0.19041.0` compiles clean with no
  warnings. Android additionally needs the Android SDK and a JDK; iOS and MacCatalyst need a
  Mac. Neither has been tried here, so "cross-platform" is a target list, not a tested claim.
- **It has never been run.** There is no device or emulator in the loop, so no screen has been
  displayed and the survey flow has never been exercised end to end.
- **Two known faults would stop it working**, both listed under Known problems below.
  Compiling is not the same as working, and in this case it is a good way short of it.

The server side it depends on *is* tested - see
`StewardshipSurvey.Tests/Integration/BearerAuthTests.cs` and `SurveyApiTests.cs`.

## Features

Implemented in code, unverified at runtime:

- Login with email and password, with the token held in platform secure storage
- Automatic sign-in on startup from the stored token
- Profile viewing and editing
- Interest, involvement and service-role selection
- Token refresh, and a logout that ends the session on the server

Built on the MVVM Community Toolkit, with dependency injection, async throughout, and Polly
retry on transient HTTP failures.

## Project Structure

```
StewardshipSurvey.Maui/
+-- MauiProgram.cs                          # Application entry point & DI
+-- App.xaml, App.xaml.cs                   # Application resources
+-- AppShell.xaml, AppShell.xaml.cs         # Navigation shell and route registration
|
+-- Pages/
|   +-- LoginPage.xaml(.cs)                 # Login screen
|   +-- MemberInfoPage.xaml(.cs)            # Profile information
|   +-- SelectInterestsPage.xaml(.cs)       # Interest selection
|   +-- SelectInvolvementsPage.xaml(.cs)    # Involvement selection
|   +-- SelectServiceRolesPage.xaml(.cs)    # Service role selection
|
+-- ViewModels/
|   +-- LoginViewModel.cs                   # Login logic
|   +-- MemberInfoViewModel.cs              # Profile logic
|   +-- SelectInterestsViewModel.cs         # Interest selection logic
|   +-- SelectionViewModels.cs              # Involvement and service-role logic
|
+-- Services/
|   +-- MemberApiService.cs                 # API communication
|   +-- AuthenticationService.cs            # Authentication & token management
|
+-- Models/
|   +-- Dtos.cs                             # Data transfer objects
|
+-- Converters/
|   +-- ValueConverters.cs                  # Bindings the XAML relies on
|
+-- Platforms/
|   +-- Android/                            # MainActivity, MainApplication, manifest
|   +-- iOS/                                # AppDelegate, Program, Info.plist
|   +-- MacCatalyst/                        # AppDelegate, Program, Info.plist
|   +-- Windows/                            # WinUI App, app.manifest
|
+-- Resources/
|   +-- AppIcon/                            # appicon.svg, appiconfg.svg
|   +-- Splash/                             # splash.svg
|   +-- Fonts/                              # empty - see the README there
|
+-- StewardshipSurvey.Maui.csproj           # Project configuration
```

## Building

```bash
dotnet workload install maui
dotnet build StewardshipSurvey.Maui/StewardshipSurvey.Maui.csproj -f net8.0-windows10.0.19041.0
```

This project is deliberately not in `StewardshipSurvey.sln`: CI runs on `ubuntu-latest`, which
has no MAUI workloads, so including it would break the build for the web application. Nothing
builds or tests this project automatically, so build it locally before trusting a change.

Start the web application first - the client expects it at `https://localhost:7295`, which
matches the `https` launch profile in `StewardshipSurvey/Properties/launchSettings.json`. That
address is hardcoded in two places, `MauiProgram.cs` and `Services/AuthenticationService.cs`,
and both need changing together.

## API Endpoints

Everything except the three catalogue lists requires a bearer token. The API does not accept
the web application's sign-in cookie.

### Authentication
- `POST /api/auth/login` - email and password, returns a token
- `POST /api/auth/refresh` - exchanges a token for a fresh one
- `POST /api/auth/logout` - invalidates every outstanding token for the account

### Member information
- `GET /api/members/current` - the signed-in member's profile
- `PUT /api/members/current` - save the signed-in member's profile
- `GET /api/members/{id}` - another member's profile; owner, Staff or Admin only

### Interests, involvements and service roles
The same three routes for each of `interests`, `involvements` and `serviceroles`:
- `GET /api/{area}/all` - the catalogue. Public, so the survey can be shown before sign-in
- `GET /api/{area}/current` - the signed-in member's selections
- `POST /api/{area}/current` - replace the signed-in member's selections

### Reporting
- `GET /api/reports/members` - the member report. Staff or Admin

## Authentication Flow

1. **Login**
   - User enters email and password
   - Credentials sent to `/api/auth/login`
   - Server returns JWT token
   - Token stored securely in device storage

2. **Authenticated Requests**
   - Token included in `Authorization: Bearer {token}` header
   - API validates the token and processes the request
   - Token is valid for one hour (`Jwt:AccessTokenMinutes`)

3. **Refresh**
   - `POST /api/auth/refresh` exchanges a token for a fresh one
   - Works for up to seven days past expiry (`Jwt:RefreshWindowMinutes`)
   - Refused if the account was deactivated, had its roles changed, logged out, or is locked

4. **Logout**
   - `POST /api/auth/logout` invalidates every outstanding token for the account
   - Token removed from secure storage
   - User redirected to login page

The client implements all of this: `AuthenticationService.RefreshAsync` renews the token,
`LogoutAsync` calls the server before clearing locally, and `MemberApiService` retries once on
a 401 rather than reporting an empty result.

## Data Models

`Models/Dtos.cs` mirrors the server's `Models/DTOs/`. The two area types are similar but not
interchangeable - note the differing name field:

```csharp
public class InterestAreaDto
{
    public int InterestAreaID { get; set; }
    public string InterestArea { get; set; }      // name field
    public string Description { get; set; }
    public bool IsActive { get; set; }
}

public class InvolvementAreaDto
{
    public int InvolvementAreaID { get; set; }
    public string AreaOfInvolvement { get; set; } // name field
    public string Description { get; set; }
    public bool IsActive { get; set; }
}
```

`MemberDto` carries the profile fields: name, email, the three phone numbers, address, contact
preferences (`PrefersPhone`, `PrefersEmail`, `PrefersText`), `Sex`, `Comments` and `IsActive`.

`MemberInterestDto`, `MemberInvolvementDto` and `MemberServiceRoleDto` are declared but never
deserialized into - `MemberApiService` reads the `.../current` responses as the area types
above and uses only the id. It works, but the client and the server disagree about the shape
of those responses.

## Fonts and theming

Colours are `ResourceDictionary` entries in `App.xaml`.

There are no font files in this repository. `MauiProgram` used to call `ConfigureFonts` naming
two `.ttf` files that had never existed, so every control fell back to the platform default;
the call was removed rather than left pointing at nothing. To add a font, see
`Resources/Fonts/README.md` - the csproj already globs that folder.

## Known problems

Neither of these is a documentation gap. Both are real, and neither is fixed.

1. **Saving a profile cannot work.** `Services/MemberApiService.cs:294` sends
   `POST /api/members/current`. The server exposes `PUT` for that route
   (`Controllers/Api/MembersController.cs:82`), so the call gets a 405.

2. **The login page would fail to load.** `Pages/LoginPage.xaml` and `Pages/MemberInfoPage.xaml`
   bind `StringToBoolConverter` and `StringToValueConverter`. Neither exists in
   `Converters/ValueConverters.cs` and neither is registered in `App.xaml`, which holds only
   `StringNotEmptyConverter` and `InvertedBoolConverter`. An unresolved `StaticResource` is a
   runtime failure in MAUI rather than a build error, which is exactly why this project builds
   clean and would still fall over on the first screen. The contact-preference radio buttons on
   the profile page depend on the second converter.

Both are tracked in the repository's [`TODO.md`](../TODO.md).

## Version Info

- .NET 8.0
- .NET MAUI 8.0.100
- MVVM Community Toolkit 8.2.2
- Polly 8.4.1
