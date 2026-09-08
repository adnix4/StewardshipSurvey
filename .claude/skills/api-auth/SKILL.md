---
name: api-auth
description: How the Stewardship Survey authenticates - the Identity cookie for Razor Pages, bearer tokens for the API, and how the two coexist. Use when adding or changing an API controller, touching [Authorize], working on tokens, claims, roles or revocation, or writing a test that needs an authenticated caller.
---

# Authentication in the Stewardship Survey

Two schemes, one decision point.

```
Request
  └─ Authorization: Bearer <token>?  ─ yes ─→ JWT bearer handler
                                     ─ no  ─→ Identity cookie handler
```

That choice is made by a **policy scheme** in `Program.cs`, registered as the default:

```csharp
.AddPolicyScheme(AuthSchemes.CookieOrBearer, "...", options =>
{
    options.ForwardDefaultSelector = context => /* bearer header? */;
})
```

**Why the header and not the path.** An `/api` prefix looks like the obvious selector and is
worse. The API accepts cookie-authenticated callers today and this project's own
`MembersApiTests` rely on it, so a path-based selector would have broken seven passing tests
to no benefit. Header-based selection is additive.

**The consequence worth knowing:** because the policy scheme is the *default*, a bare
`[Authorize]` does the right thing everywhere. Do not add `AuthenticationSchemes = ...` to an
attribute unless you specifically want to exclude one of the two schemes.

## Adding an API controller

Class-level `[Authorize]`, then `[AllowAnonymous]` on the specific actions that are genuinely
public. Never the other way round.

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]                     // default deny
public class ThingsController : ControllerBase
{
    [HttpGet("all")]
    [AllowAnonymous]            // catalogue data, deliberately public
    public async Task<IActionResult> GetAll() { ... }
}
```

`InterestsController`, `InvolvementsController` and `ServiceRolesController` were written the
other way round — no class-level attribute, per-action opt-in — which means an action added
without an attribute is public. Nothing was actually exposed, but the default was wrong.

## What a token carries

Minted in `Services/AccessTokenIssuer.cs`. One place on purpose: the claims a token carries and
the claims the pipeline expects cannot drift apart if only one file writes them.

| Claim | Why |
|---|---|
| `NameIdentifier` | the user id; how the stamp check finds the account |
| `Email`, `Name` | display |
| `MemberID` | so a client need not make a second call |
| `Role` (one per role) | `[Authorize(Roles=...)]` and `User.IsInRole` |
| `sstamp` | the security stamp — this is what makes the token revocable |

## Revocation — the part that is easy to get wrong

A JWT cannot be recalled. Nothing about signing it gives you a way to take it back. Revocation
here works because the security stamp is baked into the token and re-checked on every request
(`JwtBearerEvents.OnTokenValidated`).

So: **anything that should end a session must bump the security stamp.**

```csharp
await _userManager.UpdateSecurityStampAsync(user);
```

Deactivation does it. Logout does it. A role change must do it — otherwise the old token keeps
the old roles until it expires, because the roles in a token are a snapshot from issue time,
not a live read.

The cost is one primary-key lookup per authenticated API request. Accepted deliberately; the
alternative is a token that outlives the account it belongs to.

The granularity is the whole account. There is no "sign out this one phone" — that would need a
token table, which would need a migration, which nothing in this repo's test suite exercises.

## Configuration

`JwtOptions` (`Data/JwtOptions.cs`), bound from the `Jwt` section. `Jwt:Key` is a secret and
lives in user secrets, never in a committed appsettings file:

```bash
dotnet user-secrets set "Jwt:Key" "<random value of at least 32 characters>"
```

With no key configured the bearer handler has no signing key and rejects everything, and
`AccessTokenIssuer` throws a message naming the setting. The app still starts — a developer
without the secret gets a clear error on one endpoint, not an app that will not run.

**Never read `Jwt:*` into a local in `Program.cs`.** See the trap below.

## Traps

- **Do not read configuration eagerly at startup to make a registration decision.** The test
  host adds its configuration *after* the top-level statements run, so a value read into a
  local is the pre-test value. This cost real time: the bearer scheme configured itself with no
  signing key and rejected every token it had just issued, which looks exactly like a broken
  token rather than a broken registration. Use
  `AddOptions<T>().Configure<IOptions<JwtOptions>>(...)` so the read happens on first use.
- **`AddAuthentication(someScheme)` with an argument overwrites the default scheme.** Doing that
  with the bearer scheme would make every Razor page challenge via JWT, turning the whole
  signed-out browsing experience into 401s. `BearerAuthTests` has a test guarding exactly this.
- **`PasswordSignInAsync` issues a cookie**; `CheckPasswordSignInAsync` does not. The API wants
  the second one. Both do identical lockout bookkeeping and return the same `SignInResult`, so
  the `IsLockedOut` / `IsNotAllowed` branches are unaffected — the only difference is the
  cookie. The API used to use the first and so had two auth mechanisms while validating neither.
- **`Forbid()` and `Challenge()` go through the cookie handler's redirect events** unless the
  bearer handler is the one that ran. For an API path that means a 302 to an HTML login page,
  which a JSON client cannot read. `Program.cs` converts those to 401/403 JSON for `/api`, and
  `MembersController` returns an explicit `StatusCode(403)` for the same reason.

## Testing

`.claude/skills/testing` has the harness detail. In short:

- `CreateBearerClientAsync(email)` — the MAUI path, token only, no cookie.
- `CreateSignedInClientAsync(email)` — cookie; still valid against `/api`.
- `IssueTokenAsync(email)` + `CreateClientWithToken(token)` — to prove a token that worked a
  moment ago has stopped working.

A test that changes a user and expects their existing token to behave differently must bump the
security stamp, not just make the change.

## Known gap

The API accepts a cookie on state-changing endpoints (`PUT /api/members/current` and the three
`POST .../current`) with no antiforgery token. Blunted by the closed CORS policy, and it
predates the bearer work. The clean fix is a bearer-only API; it is in `TODO.md` section 3
because it means rewriting the cookie-authenticated `MembersApiTests` and should be done
deliberately.
