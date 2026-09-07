# Outstanding issues

Working document, ordered by severity. Each item names the file and line so a fresh session
can go straight to the code without re-deriving anything.

Ticked items are kept rather than deleted: what was wrong and why it was wrong is the
useful part, and several of these were found while fixing something else.

State: `main`, 100 tests, 0 warnings, CI green. **Section 1 is empty.**

---

## 1. Security — all clear

- [x] ~~**Any signed-in user can read every member's personal details.**~~ Fixed.
  `GET /api/members/{id}` now returns 403 unless the id is the caller's own `MemberID` or the
  caller holds Staff/Admin. The check runs *before* the database lookup, so a 403 for a
  forbidden id is indistinguishable from a 403 for a missing one and the id space cannot be
  mapped. No caller was broken: the MAUI client only ever used `/current`.
  Covered by `StewardshipSurvey.Tests/Integration/MembersApiTests.cs` — 8 tests, the first API
  coverage in the project. Teeth verified by neutralising the guard: the three attack tests
  fail without it.
  Note: `Forbid()` was the first attempt and is wrong here — the cookie handler turns it into
  a 302 to Access Denied, which a JSON client cannot read. An explicit
  `StatusCode(403)` replaced it, with the reason recorded at the call site.

- [x] ~~**API login allows unlimited password guessing.**~~ Fixed, and it was worse than
  written here. My triage claimed the Razor path passed `true`. It did not — `Login.cshtml.cs:115`
  passed `false` as well, with the scaffolded "This doesn't count login failures" comment still
  attached. So **the whole application had no brute-force protection**, and the `IsLockedOut`
  branches in both files were unreachable code.
  Both call sites now pass `true`. The policy is stated explicitly in `Program.cs` rather than
  inherited invisibly: 5 attempts, 15-minute lockout, applied to new users.
  The API also gained an `IsLockedOut` branch (423) and an `IsNotAllowed` branch (unconfirmed
  address), which previously both surfaced as "Invalid email or password".
  Covered by `LockoutTests` in `StewardshipSurvey.Tests/Integration/AuthHardeningTests.cs` —
  7 tests, including one asserting the two paths share a single counter. Teeth verified: 6 of
  the 7 fail with the flag reverted.

- [x] ~~**The profile form binds the EF entity directly (overposting).**~~ Fixed.
  The page now binds `Models/DTOs/MemberProfileInput.cs`, which declares only the 17 fields the
  form owns. `MemberID`, `IsActive`, `CreatedDate`, `ApplicationUserID` and the three
  navigation collections are no longer reachable from a request.
  The insert path was the live one: it called `Add(MemberDetails)` on the bound object as it
  arrived. The update path already copied a fixed field list, which blunted it. Both now go
  through `MemberProfileInput.ApplyTo`, so the two cannot drift apart — that duplication was
  itself a listed Medium item.
  Covered by `ProfileOverpostingTests` (3 tests). Teeth verified: 2 fail when the hole is
  reopened. `MembershipStatus` stays member-editable — that is the radio button on the form,
  by design, not an escalation.
  Note: `MembershipStatus` is deliberately in the input model. A member choosing their own
  status is the intended behaviour; the admin can override it on the Edit User page.

- [x] ~~**CSV formula injection.**~~ Fixed. `CsvField` now prefixes `'` to any value whose
  first non-whitespace character is `=`, `+`, `-`, `@`, tab or CR. Quoting alone never helped:
  a spreadsheet strips the CSV quotes before deciding whether a cell is a formula.
  Leading whitespace is looked past, so padding cannot smuggle a value through.
  Accepted cost: the apostrophe is visible in some importers, so a phone number typed as
  `+1 555 0100` will show it. Running whatever a member typed into a free-text box on a staff
  machine is the worse outcome.
  `MemberReportHelperTests` extended to 29 tests. Teeth verified: 10 fail with the trigger
  list emptied.

- [x] ~~**CORS is wide open, and registered in the wrong order.**~~ Fixed. The middleware is
  no longer registered at all unless `Cors:AllowedOrigins` lists something — the honest
  default, because the only API client is the native MAUI app, which sends no `Origin` header
  and to which CORS never applied. The old policy therefore permitted everything and protected
  nothing, and could not have served a browser client anyway: `AllowAnyOrigin` cannot be
  combined with credentials, and this API authenticates by cookie.
  A configured origin gets `WithOrigins(...).AllowAnyMethod().AllowAnyHeader().AllowCredentials()`.
  Order corrected to `UseRouting` → `UseCors` → `UseAuthentication` → `UseAuthorization`.
  `CorsTests` — 4 tests covering both the closed default and the switch actually working.
  **Honest limit:** no test covers the ordering. With one blanket policy and no `[EnableCors]`
  attributes anywhere, the misordering had no observable effect. It is fixed because the order
  is a documented requirement, not because a test demanded it.

---

## 2. High — does not work

- [ ] **The MAUI app cannot authenticate at all.**
  `StewardshipSurvey/Program.cs` (no `AddJwtBearer` anywhere) vs
  `StewardshipSurvey.Maui/Services/MemberApiService.cs:42,70,106,134,170,198`
  The client sends `Authorization: Bearer` on every call; the server registers no bearer
  scheme, so `[Authorize]` resolves to the Identity cookie and the header is ignored. All 15
  API endpoints across 6 controllers are unreachable from mobile.
  Also `AuthController.cs:108-114` emits no role claims, so even with a bearer scheme
  `[Authorize(Roles = "Staff,Admin")]` on `ReportsController.cs:12` could never pass. Tokens
  last 7 days with no refresh or revocation. `AuthController.cs:51` additionally issues a
  cookie as a side effect, so the endpoint has two auth mechanisms and validates the wrong one.
  `StewardshipSurvey.Maui/README.md:26` advertises this as working.

- [ ] **Survey pages turn failures into success and can wipe saved answers.**
  `StewardshipSurvey/Pages/Members/SelectInterests.cshtml.cs:69-74`
  `StewardshipSurvey/Pages/Members/SelectMemberServiceRoles.cshtml.cs:77-82`
  (`SelectMemberInvolvement` follows the same template)
  Catches `Exception`, logs, then `return Page()` — the user sees an empty form with no error,
  and the next POST saves that emptiness over their real selections.
  Also `SelectInterests.cshtml.cs:120` does `await OnGetAsync(); return Page();`, **discarding
  the `IActionResult`**, so an `Unauthorized()` becomes a 200.

- [ ] **The testing skill contradicts the code.**
  `.claude/skills/testing/SKILL.md:97`
  Still says registration cannot complete and warns against posting the registration form —
  which is exactly what `StewardshipSurvey.Tests/Integration/RegistrationTests.cs` now does.
  Stale since the email-confirmation fix.

- [x] ~~Merge `feature/email-confirmation` into `main` and delete the branch.~~ Done: merged
  fast-forward to `6de5cc0`, branch deleted locally and on the remote, CI green on `main`.

---

## 3. Medium — correctness and coverage

- [ ] **CSV Status column is dead.** `MemberReport.cshtml.cs:137` filters
  `.Where(m => m.IsActive)`; line 180 then computes `member.IsActive ? "Active" : "Inactive"`.
  Always "Active".

- [ ] **Role changes applied without checking the result.**
  `Pages/Admin/EditUser.cshtml.cs:128,130` (also 220, 227) discard the `IdentityResult` and
  redirect as success. `DeactivatedUserPurgeService` does check — the codebase is inconsistent.

- [ ] **No API test coverage at all.** 15 endpoints, 6 controllers, 0 tests. Partly blocked by
  the bearer-auth item above.

- [ ] **Report logic exists in triplicate.** (Unrelated to the profile-form duplication,
  which the overposting fix removed.) `Controllers/Api/ReportsController.cs:35-82`,
  `MemberReport.cshtml.cs:68-120` and `OnGetExport` repeat the same include/filter/sort chain.
  `ParseIntList` is duplicated byte-for-byte between `ReportsController.cs:100-109` and
  `MemberReport.cshtml.cs:198-207`, double-parse bug included (TryParse then Parse).

- [ ] **Three API controllers are anonymous-by-default.** `InterestsController`,
  `InvolvementsController`, `ServiceRolesController` have no class-level `[Authorize]` — only
  per-action opt-in, so any action added without an attribute is public.

- [ ] **Nav lags after an admin changes membership status.** Role claims live in the auth
  cookie; updates on re-login or at the next security-stamp validation (30 min default). Page
  guards read the database, so cosmetic only, never an access hole.

- [ ] **Two accounts have the `Member` role with a null `MembershipStatus`** —
  `fshromen@yahoo.com`, `tim@yahoo.com`. Predates the mirrored-role design; self-heals on save.

- [ ] **CSV built with `+=` in a loop.** `MemberReport.cshtml.cs:174,182` — O(n²) on a full
  export. `StringBuilder` is used elsewhere (`Services/FileDropEmailSender.cs:47`).

- [ ] **Logging uses string interpolation with user input.** `AuthController.cs:47,54,59,77`
  and throughout `Pages/Members/*` — unescaped email text reaches the log stream and the
  message formats even when the level is disabled. Structured templates are used correctly
  elsewhere, e.g. `MemberInfo.cshtml.cs:137,162`.

---

## 4. Low — hygiene

- [ ] **`Models/DbContext.cs`** — empty stub class that shadows
  `Microsoft.EntityFrameworkCore.DbContext` for any file importing `StewardshipSurvey.Models`
  without the EF namespace. Nothing uses it.

- [ ] **`MemberInfo.ApplicationUserID`** (`Models/MemberInfo.cs:58`) — required column that no
  code ever assigns; always `''`. Looks like a foreign key and isn't. The real link is
  `AspNetUsers.MemberID`, configured at `Data/ApplicationDbContext.cs:25-28`.

- [ ] **Remove the scaffolding leftovers — but in this order.**
  `Data/ApplicationUser.cs:2` imports
  `Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General`, and the
  `Microsoft.VisualStudio.Web.CodeGeneration.Design` package (`.csproj:27`) is scaffolding-only.
  **Trap:** `System.IdentityModel.Tokens.Jwt`, used by `AuthController`, currently arrives only
  transitively through that package. Add it as an explicit `PackageReference` *first*, or the
  build breaks in a way that looks unrelated.

- [ ] **Raw `new HttpClient()` registered as scoped.** `Program.cs:72-86`, two lines below the
  `AddHttpClient("ApiClient")` that exists to prevent exactly that.

- [ ] **Misleading `[Key]` attributes on join entities.** `Models/MemberInterest.cs:8-9` and
  `Models/MemberInvolvement.cs:31-32` declare surrogate keys that the fluent `HasKey` overrides
  (`ApplicationDbContext.cs:31-32,44-45`), leaving `MemberInterestID`/`MemberInvolvementID` as
  always-zero columns. `Models/MemberServiceRole.cs` has the correct shape — the three are
  inconsistent.

- [ ] **`DeactivatedUserPurgeService.StartupDelay`** (`Services/DeactivatedUserPurgeService.cs:19`)
  is a hardcoded minute with no clock abstraction; `DateTime.UtcNow` is called directly at
  line 71 for the cutoff. This is why `PurgeAsync` had to be widened to `internal` for testing.

- [ ] **Test factory removes all hosted services.**
  `StewardshipSurvey.Tests/Infrastructure/StewardshipWebApplicationFactory.cs:62-64` — the
  comment names the purge sweep, the call removes every `IHostedService`.

- [ ] **The MAUI project is not in `StewardshipSurvey.sln`**, so API changes never break its
  build. Deliberate for CI (Linux lacks the workloads) but surprising locally.

- [ ] **`StewardshipSurvey.Maui/README.md`** has a mojibake directory tree — box-drawing
  characters written in the wrong encoding, rendering as `?`.

- [ ] **`Pages/Index.cshtml:45-88`** — ~43 lines of commented-out "How / When / Why" scripture
  sections. Verified they do **not** leak onto the rendered page. May be a deliberate
  placeholder for planned content — ask before removing.

- [ ] **`ApplicationDbContext.cs:17`** has a commented-out `DbSet<ServiceRoles>` while
  `Models/ServiceRoles.cs` survives unmapped and unreferenced.

- [ ] **`AddRazorSupportForMvc=true`** (`.csproj:8`) is intended for class libraries shipping
  Razor views, not a Web SDK project.

---

## 5. Development database cleanup

- [ ] **Five leftover test accounts** from verification runs: `devlink-…`, `diag-…`, `rctest-…`,
  `smtpfail-…`, `verify-…` (all `@stmark.local`).
- [ ] **`staff@stmark.local` is polluted** — its profile reads "Test Staffer" with contact
  preference Email. It had no name before.
- [ ] **Rotate `Jwt:Key`** in local user secrets. Development-only: it signs tokens for a
  LocalDB instance on one machine, is not in the repository, and nothing validates those
  tokens yet (see the MAUI item in section 2). Worth doing before anything is deployed.

---

## Known limits — documented, not defects

Recorded in `.claude/skills/testing/SKILL.md`:

- The purge's delete order is not pinned by any test. `AspNetUsers.MemberID` is a `NO ACTION`
  foreign key, but EF nulls it on the tracked user during `SaveChanges`, so either order
  passes. Confirmed by deliberately inverting it.
- `EnsureCreated()` builds the test schema from the EF model, so the SQL Server migrations are
  never exercised.

---

## Why this file was not committed until now

`adnix4/StewardshipSurvey` is public, and section 1 used to describe a live, unfixed way to
extract congregation members' home addresses and birth dates. Publishing that would have been
disclosure rather than documentation, so the file was kept in `.gitignore` while the fixes
were written.

Every item in section 1 is now fixed, tested and pushed, so the reason is gone and the file is
tracked. What remains is a record of things that do not yet work well — which is ordinary
engineering documentation, not a set of instructions for attacking anyone.
