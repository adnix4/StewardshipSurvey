<header>
  
<!-- -->
# Stewardship Survey

_A .NET project with Razor Pages_

</header>

### Description

<p>
  This program is designed to help my local church (or other organizations) create a custom database to track and organize volunteers and areas that they are interested in. Future additions will include searchable forms that display volunteer information based on the areas you need help with. 
</p>

<p>
  Personal information is only accessible by the individual logged in or users with permissions granted to see it. All users must log in to see their own information and update their profiles.
</p>


### Local setup

No credentials are stored in this repository. The development accounts and the JWT
signing key are read from configuration, and the app uses
[.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) to
keep them off disk in the project folder. `UserSecretsId` is already declared in
`StewardshipSurvey.csproj`, so no additional setup is required.

Run the following from the `StewardshipSurvey` project folder, choosing your own values.
Passwords must satisfy the default ASP.NET Identity policy: at least 8 characters with
an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.

```powershell
dotnet user-secrets set "SeedAccounts:Admin:Email"                 "admin@stmark.local"
dotnet user-secrets set "SeedAccounts:Admin:Password"              "<choose a password>"
dotnet user-secrets set "SeedAccounts:Staff:Email"                 "staff@stmark.local"
dotnet user-secrets set "SeedAccounts:Staff:Password"              "<choose a password>"
dotnet user-secrets set "SeedAccounts:VolunteerOrganizer:Email"    "vol@stmark.local"
dotnet user-secrets set "SeedAccounts:VolunteerOrganizer:Password" "<choose a password>"

# Random 48-byte signing key for the API's JWTs (must be at least 32 bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 })))
```

Then `dotnet run`. On startup `AdminSeeder` creates the four roles and any seed accounts
it finds in configuration. Seeding is **Development only** — a real deployment provisions
its administrator separately.

If no seed accounts are configured the app still starts and logs a warning, so the site
runs after a fresh clone without any secrets set. The API's `/api/auth/login` endpoint
requires `Jwt:Key` and returns an error naming the missing key if it is absent.

Credentials do not belong in `appsettings.Development.json` — that file is tracked by git.
Use user secrets, or an untracked `appsettings.Development.Local.json`.
