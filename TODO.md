# Outstanding issues

Working document, ordered by severity. Each item names the file and line so a fresh session
can go straight to the code without re-deriving anything.

Ticked items are kept rather than deleted: what was wrong and why it was wrong is the
useful part, and several of these were found while fixing something else.

State: `main`, 117 tests, 0 warnings, CI green. **Sections 1 and 2 are clear.**

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

- [x] ~~**The MAUI app cannot authenticate at all.**~~ Fixed, server side.
  A policy scheme (`Program.cs`) now picks per request: an `Authorization: Bearer` header goes
  to the JWT handler, anything else to the Identity cookie. Selecting on the header rather
  than on an `/api` path prefix keeps the change purely additive — the seven `MembersApiTests`
  that authenticate the API by cookie still pass untouched — and means no `[Authorize]`
  attribute has to name a scheme, so a controller added later cannot forget to.
  Tokens now carry role claims, so `[Authorize(Roles = "Staff,Admin")]` on `ReportsController`
  is reachable at all for the first time; it was not merely protected before, it was
  unreachable. Expiry drops from 7 days to 1 hour, with `POST /api/auth/refresh` gated on the
  security stamp, so the overall sign-in window is unchanged while the credential on the wire
  is good for an hour.
  Revocation works through the Identity security stamp: the stamp is a claim in the token and
  `OnTokenValidated` compares it to the stored one, so deactivation, a role change or logout
  all kill outstanding tokens. Chosen over a refresh-token table because that needs a
  migration and **no test in this repo exercises a migration** (`EnsureCreated`), so CI could
  not validate it.
  `AuthController` login moved from `PasswordSignInAsync` to `CheckPasswordSignInAsync` — the
  same lockout bookkeeping without the cookie the API had been issuing as a side effect. All
  7 `LockoutTests` stay green, verified both ways.
  An unauthenticated `/api` call now answers 401 JSON instead of 302 to an HTML login form.
  The existing coverage for that asserted only "not 200", which a redirect satisfies; it is
  now an equality assertion.
  Covered by `StewardshipSurvey.Tests/Integration/BearerAuthTests.cs` — 11 tests. Teeth
  verified one break at a time: stripping role claims reds only the roles test; neutralising
  the stamp check reds revocation and logout; restoring `PasswordSignInAsync` reds the
  no-cookie test; removing the `/api` 401 handling reds both anonymous tests; removing the
  refresh stamp check reds only the refresh test.
  **Trade-off, stated rather than buried:** the stamp check costs one primary-key lookup per
  authenticated API request. Accepted — the alternative is a token that outlives the account.
  **Honest limits:** nothing exercises the MAUI client end to end, and "log out everywhere" is
  the only revocation granularity a stateless token design offers — there is no way to revoke
  one device.
  Note: a first attempt read `Jwt:Key` into a local at startup to decide whether to register
  the scheme. That looked equivalent and was not — the test host adds its configuration after
  the top-level statements run, so the handler configured itself with no key and rejected
  every token it had just issued. Now configured through the options system, which also
  removed the need for the registration gate entirely. Recorded in the testing skill.

- [x] ~~**Survey pages turn failures into success and can wipe saved answers.**~~ Fixed across
  all three steps — `SelectInterests`, `SelectMemberInvolvement`, `SelectMemberServiceRoles`.
  A failed load now sets `LoadFailed`, empties the option list and adds a model error; the
  views render that error and **omit the form entirely**. That is the fix for the wipe: an
  empty checkbox list is byte-for-byte the same POST as "I unticked everything", so the only
  safe thing is to not offer the form at all. The views had no validation summary either, so
  `AddModelError` had been writing to something nothing rendered — the error was invisible
  even where the code remembered to add one.
  The POST path no longer calls `OnGetAsync()` to redraw. That call was the source of the
  discarded `IActionResult`, and it also replaced the member's unsaved selections with the
  stored ones, so a transient failure looked like their edit had been thrown away. It now
  reloads only the option list and keeps what was posted.
  Also added `.Distinct()` on the posted ids: the keys are composite `(MemberID, AreaID)`, so
  a repeated id failed on the primary key — a failure the member neither caused nor could act
  on.
  Covered by `StewardshipSurvey.Tests/Integration/SurveyFailureHandlingTests.cs` — 6 tests in
  two classes. Teeth verified individually: neutralising `RecordLoadFailure` reds all 3 load
  tests, forcing the form to render reds the suppression assertion on its own, hiding the
  validation summary reds the save test, restoring the `OnGetAsync()` redraw reds the
  redisplay test, and dropping `Distinct` reds the duplicate test.
  **Honest limit:** the discarded-`IActionResult` bug has no test. To reach it, `OnPost` had
  to get past its own `MemberID` check and then throw, while `OnGet` failed the same check for
  the same user — which cannot happen in one request. It is fixed by deleting the call, not
  by a test.
  Note: `OnPostAsync` on `SelectMemberServiceRoles` still has no prospective-member guard —
  see the new item in section 3. It was out of scope here and predates this change.

- [x] ~~**The testing skill contradicts the code.**~~ Fixed. The registration entry now
  describes what actually happens: registration completes, `RequireConfirmedAccount = true`
  means the account cannot sign in until confirmed, and the factory's `FileDropEmailSender`
  writes the message where a test can read it — which is what `RegistrationTests` does.
  `CreateUserAsync` is still the right default; posting the form is for testing registration
  itself. Also added `MailDropPath` to the factory's documented surface, and a "Provoking a
  failure" section recording the two techniques the new survey tests use — including that the
  table-rename one is schema damage and must stay in a class of its own.

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

- [ ] **API test coverage is still thin.** No longer blocked — the bearer-auth item above is
  done and `MembersApiTests` (7) plus `BearerAuthTests` (11) now cover authentication and the
  members endpoints. The other 12 endpoints across `Interests`, `Involvements`, `ServiceRoles`
  and `Reports` still have no behavioural coverage of their own.

- [ ] **The API accepts a cookie on state-changing endpoints with no antiforgery token.**
  `PUT /api/members/current` and the three `POST .../current` endpoints authenticate by cookie
  as well as by bearer, and `[ApiController]` applies no antiforgery check — so a browser that
  is signed in attaches the cookie automatically. Largely blunted today by the closed CORS
  policy, which stops a cross-site JSON request being sent at all, and it predates the
  bearer-auth work rather than being introduced by it. The clean fix is to make the API
  bearer-only, which would mean rewriting the seven cookie-authenticated `MembersApiTests`;
  worth doing deliberately rather than as a side effect. Found while designing the bearer fix.

- [ ] **The MAUI client has not caught up with the server.**
  `StewardshipSurvey.Maui/Services/AuthenticationService.cs` never calls `/api/auth/logout`
  and has no refresh call; `MemberApiService.cs` turns a 401 into empty data rather than
  sending the member back to sign in. With the access token now lasting an hour instead of a
  week, a session will silently stop returning data after an hour. Recorded in the MAUI README
  too. The project is not in the solution and CI never builds it, so nothing catches this.

- [ ] **Report logic exists in triplicate.** (Unrelated to the profile-form duplication,
  which the overposting fix removed.) `Controllers/Api/ReportsController.cs:35-82`,
  `MemberReport.cshtml.cs:68-120` and `OnGetExport` repeat the same include/filter/sort chain.
  `ParseIntList` is duplicated byte-for-byte between `ReportsController.cs:100-109` and
  `MemberReport.cshtml.cs:198-207`, double-parse bug included (TryParse then Parse).

- [ ] **`SelectMemberServiceRoles` guards the GET but not the POST.**
  `Pages/Members/SelectMemberServiceRoles.cshtml.cs` — `OnGetAsync` redirects a prospective
  member away via `IsProspectiveMemberAsync`; `OnPostAsync` has no such check, so a direct
  POST saves service roles for someone the step does not apply to. Not an escalation — they
  can only write their own rows — but the guard is described as "the real guard" and only
  covers half the page. Found while fixing the failure-handling item in section 2.

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
