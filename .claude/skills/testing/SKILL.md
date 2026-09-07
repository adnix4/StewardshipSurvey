---
name: testing
description: Write, run and debug unit and integration tests for the Stewardship Survey app. Use whenever asked to test something, add coverage, reproduce a bug as a test, or diagnose a failing test. Covers the xUnit project layout, the WebApplicationFactory harness, and the traps specific to this codebase.
---

# Testing the Stewardship Survey

Tests live in `StewardshipSurvey.Tests`, which is in `StewardshipSurvey.sln`.

```
StewardshipSurvey.Tests/
  Infrastructure/   StewardshipWebApplicationFactory, AntiforgeryToken
  Unit/             pure logic, no database, no HTTP
  Integration/      anything that needs the running app
```

## Running

```bash
dotnet test                                              # everything
dotnet test --filter "FullyQualifiedName~PurgeTests"     # one class
dotnet test --filter "FullyQualifiedName~Unit"           # just the fast ones
dotnet test -v n                                         # see assertion detail on failure
```

Run from the repo root (`C:\Stewardship\MemberSurvey`). Running from inside a project
directory gives `MSB1009: Project file does not exist`.

## Which folder

`Unit/` if the code under test is reachable without a database or an HTTP context — pure
functions and mapping logic. `Integration/` for anything else. There are no interfaces in
this project and the helpers are static classes, so there are no seams to mock: if it touches
data, drive it through the factory rather than trying to fake `ApplicationDbContext`.

## Writing an integration test

Take the factory as a class fixture. Never build your own host.

```csharp
public class MyTests : IClassFixture<StewardshipWebApplicationFactory>
{
    private readonly StewardshipWebApplicationFactory _factory;
    public MyTests(StewardshipWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Something()
    {
        var email = $"user-{Guid.NewGuid():N}@stmark.local";
        await _factory.CreateUserAsync(email, status: MembershipStatus.Member, roles: Roles.Staff);
        var client = await _factory.CreateSignedInClientAsync(email);
        ...
    }
}
```

The factory gives you:

- `CreateNonRedirectingClient()` — anonymous, does not follow redirects.
- `CreateUserAsync(email, password, status, roles)` — confirmed account, `RegisteredUser`
  always added, optional profile and mirrored status role.
- `CreateSignedInClientAsync(email)` — signs in through the real Identity UI, returns a
  client holding a genuine auth cookie.
- `WithScopeAsync(provider => ...)` — a fresh DI scope for arranging or asserting against
  the database.

**Generate unique emails per test.** The fixture is shared across a class, so the database is
shared too. A hardcoded address collides the moment a second test uses it.

**Every POST needs an antiforgery token.** Fetch the page, then
`AntiforgeryToken.Extract(html)`. Without it you get a 400 that looks like a routing problem.

## Traps specific to this app

Each of these cost real time to find. They are handled inside the factory — do not undo them.

- **The environment must not be `Development`.** `AdminSeeder.SeedAsync` runs at startup in
  Development, hits the database before the pipeline exists, and throws a bare `Exception` on
  failure. `WebApplicationFactory` defaults to Development, so the factory forces `Testing`.
- **Remove both DbContext descriptors**, `DbContextOptions<ApplicationDbContext>` *and*
  `ApplicationDbContext`. Dropping only the options leaves SQL Server registered and EF
  reports two providers.
- **Keep the SQLite connection open.** A `:memory:` database is destroyed when its last
  connection closes.
- **`AllowAutoRedirect = false`**, or the app's `UseHttpsRedirection` and the login redirects
  swallow the status codes you want to assert.
- **A 302 does not mean success.** A refused sign-in redirects to
  `/Identity/Account/Lockout`; a successful one redirects to the return URL. Assert on
  `Location`, not just the status.
- **API endpoints authenticate by cookie, not bearer token.** `AuthController` issues JWTs,
  but `Program.cs` never registers a JWT bearer scheme, so `[Authorize]` resolves to the
  Identity cookie. An `Authorization: Bearer` header is ignored. (This is a real bug in the
  app, not a testing quirk.)
- **`Models/DbContext.cs` is an empty stub class** that shadows
  `Microsoft.EntityFrameworkCore.DbContext`. A test file with `using StewardshipSurvey.Models;`
  but no `using Microsoft.EntityFrameworkCore;` gets a baffling compile error.
- **Registration cannot complete.** `RequireConfirmedAccount = true` with no email sender
  means a self-registered user can never sign in. Use `CreateUserAsync`, which sets
  `EmailConfirmed = true`, rather than posting the registration form.

## Reaching private helpers

`StewardshipSurvey.csproj` has `<InternalsVisibleTo Include="StewardshipSurvey.Tests" />`.
Four members are `internal` purely so tests can reach them: `MemberReportModel.CsvField`,
`MemberReportModel.ParseIntList`, `EditUserModel.DeactivationBlockedReasonAsync`, and
`DeactivatedUserPurgeService.PurgeAsync`.

Widening to `internal` is fine when it makes real logic testable. Making something `public`
for a test is not — that changes the API surface.

`PurgeAsync` is internal because `ExecuteAsync` sleeps a hardcoded one-minute
`StartupDelay` before doing anything; there is no injected clock, so calling `PurgeAsync`
directly is the only practical way to test the sweep.

## What the database does and does not prove

The tests run on SQLite with `Foreign Keys=True`, and `DatabaseConstraintTests` proves it:
`PRAGMA foreign_keys` returns 1, a survey answer pointing at a missing member is rejected,
and deleting a profile cascades to its answers **in the database**, not just in EF's change
tracker.

Two honest limits:

- **The purge's delete order is not pinned down by any test.** `AspNetUsers.MemberID` is a
  `NO ACTION` foreign key, so raw SQL must delete the user before the profile — but when the
  user entity is tracked, EF nulls the FK during `SaveChanges` and either order works.
  Inverting the order in `PurgeAsync` does *not* fail the suite. Do not assume it would.
- **`EnsureCreated()` builds the schema from the EF model, not the migrations.** A migration
  that is valid in EF but broken on SQL Server would not be caught here.

## Do not

- Switch to `Microsoft.EntityFrameworkCore.InMemory`. It ignores foreign keys and cascade
  deletes, so `DatabaseConstraintTests` would pass vacuously.
- Add FluentAssertions. Version 8 moved to a paid licence for commercial use; plain xUnit
  assertions are used throughout.
- Add a test that cannot fail. If a test is meant to catch a regression, break the code on
  purpose once and confirm it goes red before keeping it.
