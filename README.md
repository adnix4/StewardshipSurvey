<header>
  
<!-- -->
# Stewardship Survey

_An ASP.NET Core 8 application: Razor Pages for the web, a JSON API for the mobile client._

</header>

### Description

<p>
  This program is designed to help my local church (or other organizations) create a custom database to track and organize volunteers and areas that they are interested in. Staff can search and filter members by the interests, ministries and service roles they have signed up for, and export the result as CSV.
</p>

<p>
  Personal information is only accessible by the individual logged in or users with permissions granted to see it. All users must log in to see their own information and update their profiles.
</p>

### What is in the repository

| Project | What it is |
|---|---|
| `StewardshipSurvey` | The web application. Razor Pages for members, staff and administrators, plus a JSON API under `/api` for the mobile client. |
| `StewardshipSurvey.Tests` | xUnit unit and integration tests. |
| `StewardshipSurvey.Maui` | A .NET MAUI client. **Deliberately outside `StewardshipSurvey.sln`** — CI runs on Linux, which has no MAUI workloads, so including it would break the build for everything else. See its own [README](StewardshipSurvey.Maui/README.md). |

### Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (installed with Visual Studio; the connection string in
  `appsettings.json` points at `(localdb)\mssqllocaldb`)
- `dotnet-ef` for migrations: `dotnet tool install --global dotnet-ef`

Only if you intend to build the MAUI client: `dotnet workload install maui`.

### Local setup

No credentials are stored in this repository. The development accounts and the JWT
signing key are read from configuration, and the app uses
[.NET user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) to
keep them off disk in the project folder. `UserSecretsId` is already declared in
`StewardshipSurvey.csproj`, so no additional setup is required.

Run the following from the `StewardshipSurvey` project folder, choosing your own values.
Passwords must satisfy the default ASP.NET Identity policy: at least 6 characters with
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

Any role name from `Data/Roles.cs` works as a `SeedAccounts:` key, but only the three above
are worth seeding. `Member` and `ProspectiveMember` mirror a member's answer on their own
profile and should never be assigned directly; `RegisteredUser` is added to every account
automatically.

### Database

Startup does not apply migrations. Create the database once:

```powershell
dotnet ef database update
```

In Development the app also serves the migrations endpoint, so if the schema falls behind
you get a page offering to apply the outstanding migrations rather than an opaque error.

### Running

```powershell
dotnet run
```

On startup `AdminSeeder.EnsureRolesAsync` creates the six roles — `Admin`, `Staff`,
`VolunteerOrganizer`, `RegisteredUser`, `Member`, `ProspectiveMember`. This runs in **every**
environment, because `AddToRoleAsync` throws on a missing role and registration would fail
without them.

Seed *accounts* are a separate step and are **Development only** — a real deployment provisions
its administrator separately. If no seed accounts are configured the app still starts and logs
a warning, so the site runs after a fresh clone without any secrets set. The API's
`/api/auth/login` endpoint requires `Jwt:Key` and returns an error naming the missing key if it
is absent.

### Testing

```powershell
dotnet test
```

172 tests, and the build is kept at zero warnings. The convention in this project is that a
regression test has to be *proven* to fail against the unfixed code before it is kept — see
`.claude/skills/testing` for the harness, the traps, and what the suite does and does not
prove.

### The API

Everything under `/api` is **bearer-only**: it authenticates by JWT and does not accept the
Identity cookie, because a browser attaches a cookie automatically and these endpoints have no
antiforgery check.

- `POST /api/auth/login` — email and password, returns a token good for one hour
- `POST /api/auth/refresh` — exchanges a token for a fresh one, for up to seven days
- `POST /api/auth/logout` — invalidates every outstanding token for the account

Tokens carry the caller's roles and their Identity security stamp, so deactivating an account,
changing its roles, logging out, or rotating `Jwt:Key` all end live sessions rather than waiting
for expiry. The full endpoint list is in the
[MAUI README](StewardshipSurvey.Maui/README.md).

### Email

Registration requires the new account to confirm its email address. If no SMTP server is
configured the app writes each message to `App_Data/mail` as a `.eml` file and logs the
confirmation link, so the flow works end to end on a machine with no mail server. In
Development the confirmation link is also shown on the registration confirmation page; it is
never shown in any other environment.

To send real mail, set the SMTP details and the app switches sender automatically:

```powershell
dotnet user-secrets set "Email:From"           "noreply@yourdomain"
dotnet user-secrets set "Email:Smtp:Host"      "smtp.yourprovider.com"
dotnet user-secrets set "Email:Smtp:Port"      "587"
dotnet user-secrets set "Email:Smtp:User"      "<smtp username>"
dotnet user-secrets set "Email:Smtp:Password"  "<smtp password>"
```

`Email:Smtp:UseSsl` defaults to true and only needs setting to turn it off.

A send failure is logged as an error and rethrown rather than swallowed - a confirmation
email that silently vanishes is exactly the bug this replaced.

### A note on secrets

Credentials do not belong in `appsettings.Development.json` — that file is tracked by git.
Use user secrets, or an untracked `appsettings.Development.Local.json`.

### Further reading

- [`TODO.md`](TODO.md) — the running record of known defects and what was done about them.
  Ticked items keep the explanation of what was wrong, which is usually the useful part.
- [`scripts/`](scripts/README.md) — development database cleanup and how to rotate `Jwt:Key`.
- `.claude/skills/` — conventions for testing, Razor Pages, the API's authentication, and EF
  migrations, each written for a class of defect this project actually hit.
