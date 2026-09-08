# Outstanding issues

Working document, ordered by severity. Each item names the file and line so a fresh session
can go straight to the code without re-deriving anything.

Ticked items are kept rather than deleted: what was wrong and why it was wrong is the
useful part, and several of these were found while fixing something else.

State: `main`, 138 tests, 0 warnings, CI green. **Sections 1, 2, 4 and 5 are clear, and every
item this file was opened with is closed.** What remains is three items in section 3, all
found while fixing the others rather than present at the start.

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

- [x] ~~**CSV Status column is dead.**~~ Fixed by removing the column. The report is an
  active-members report at every call site, so the value was computed after the filter that
  made it a foregone conclusion: every row of every export ever taken said "Active". Making it
  mean something would have meant including deactivated members in a staff export, which is a
  product decision rather than a bug fix.
  Note the path in this item was wrong: the file is `Pages/Staff/MemberReport.cshtml.cs`, not
  `Pages/Admin/`.

- [x] ~~**Role changes applied without checking the result.**~~ Fixed. All four call sites go
  through a private `ApplyAsync`, which checks `.Succeeded`, logs the joined error
  descriptions and surfaces them through `ModelState` — the pattern
  `DeactivatedUserPurgeService:108-115` already used. The page had no logger at all; one is
  injected now.
  The worst of the four was `UpdateAsync` on the deactivate path: a failure there meant the
  account was never actually locked out while the screen reported it deactivated.
  **Honest limit:** no test. Making Identity fail these calls needs a store that misbehaves on
  demand, and there are no seams here to substitute one — the project mocks nothing. The
  change is a strict improvement on discarding the result, but it is unproven.

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

- [x] ~~**Report logic exists in triplicate.**~~ Fixed. `Data/MemberReportQuery.cs` now holds
  the include/filter chain, the sort and `ParseIntList`, and all three call sites use it.
  **The copies had already drifted, which is the point of the item.** `OnGetExport` accepted
  `sortColumn` and `sortAscending` and then materialised the query with no `OrderBy` at all -
  so the CSV came out in database order while the screen showed the same filters sorted. That
  was not in this list; three copies is how it stayed unnoticed. The extraction fixes it by
  construction.
  `ParseIntList` now parses each value once instead of `TryParse`-then-`Parse`, and moved off
  the page model, so its unit test no longer needs a page model built round a null context.
  Covered by `Integration/MemberReportExportTests.cs` (5) plus the existing unit tests. Teeth
  verified: removing the sort reds the sort test, restoring the Status column reds that one.

- [x] ~~**`SelectMemberServiceRoles` guards the GET but not the POST.**~~ Fixed — `OnPostAsync`
  calls the same `IsProspectiveMemberAsync` and redirects, so the helper's doc comment is now
  true of the whole page rather than half of it.
  Covered in `AdminAndGuardTests`: a prospective member posting directly is redirected and
  writes no rows. Teeth verified — removing the guard reds it.

- [x] ~~**Three API controllers are anonymous-by-default.**~~ Fixed — and it was four.
  `AuthController` had the same shape and is not in the original list; the test found it, not
  I did. All four now carry class-level `[Authorize]`, with `[AllowAnonymous]` on the five
  actions that are deliberately public (the three catalogue lists, plus login and refresh).
  **This could not be tested through HTTP.** Nothing was exposed before the fix — every
  `current` action had its own attribute — so no request behaves differently. The defect is in
  what happens next, when an action is added without one. `Unit/ApiControllerAuthorizationTests.cs`
  therefore tests it structurally: every controller in the API namespace must deny by default,
  the anonymous actions are an explicit allow-list, and a third test pins the controller names
  so a namespace rename cannot turn the other two into no-ops that pass forever.
  Teeth verified: removing a class-level attribute reds it and names the controller.

- [x] ~~**Nav lags after an admin changes membership status.**~~ Fixed, and it stopped being
  cosmetic while this was open. `EditUser.OnPostAsync` now calls `UpdateSecurityStampAsync`
  when roles or status actually changed — the same lever `OnPostDeactivateAsync:230` already
  used. Once bearer tokens became real, a stale role claim was no longer only a wrong menu: an
  outstanding API token carried the old roles until it expired.
  Guarded both ways: one test asserts the stamp moves and an existing token stops working,
  another asserts an edit that changes nothing leaves the session alone — the bump signs the
  person out everywhere, so it must not fire on an idle save. Teeth verified on the first.

- [x] ~~**Two accounts have the `Member` role with a null `MembershipStatus`.**~~ Fixed as code
  rather than as data. `MembershipStatusRoles.SyncAsync` only ever ran from a write, so a row
  never re-saved stayed inconsistent forever — "self-heals on save" was true and useless,
  because nothing was going to save them. `AdminSeeder`'s existing per-user backfill loop now
  reconciles the status role against the profile, with the same counter-and-log shape the
  `EmailConfirmed` backfill uses, so it heals on startup and cannot drift again.
  `StatusRolesAreStaleAsync` is `internal` so the check is testable without a running seeder,
  and so the log line counts real repairs rather than every account.
  **Caveat:** `AdminSeeder` runs in Development only. This repairs the developer database and
  any future one, not a production database.

- [x] ~~**CSV built with `+=` in a loop.**~~ Fixed - `StringBuilder`, matching
  `Services/FileDropEmailSender.cs:47`. Done in the same pass as the two items above because
  it is the same twenty lines. No test: the output is identical and only the copying is gone,
  so there is nothing to assert that the existing export tests do not already cover.

- [x] ~~**Logging uses string interpolation with user input.**~~ Already fixed; no code change.
  The cited lines were real when written - `git show f97ca0c:StewardshipSurvey/Controllers/Api/AuthController.cs`
  still has all four - but `e3af9df` rewrote them into structured templates while adding the
  lockout branches, and `5c714f8` did the `Pages/Members/*` half while fixing the survey
  failure handling. Both were side effects of adjacent work rather than deliberate.
  Verified empty: `grep -rE 'Log(Information|Error|Warning|Debug)\(\s*\$"' StewardshipSurvey/`
  returns nothing.

---

## 4. Low — hygiene

- [x] ~~**`Models/DbContext.cs`**~~ Deleted. Nothing referenced it. 26 of the 27 files that
  import `StewardshipSurvey.Models` do not import EF Core, so the shadow was live for all of
  them; none happened to type a bare `DbContext`, which is why it never bit.

- [x] ~~**`MemberInfo.ApplicationUserID`**~~ Removed, with a migration. Nothing ever assigned
  it, so every row held the empty string. It was created by *renaming* `PhoneNumber` in
  `20250914213506_MemberInfo` — which is how a column nobody wanted ended up with a name that
  sounded load-bearing.
  `AuthHardeningTests` used it as an overposting target; that assertion now uses `UpdatedDate`,
  which the page stamps itself and so serves the same purpose.

- [x] ~~**Remove the scaffolding leftovers.**~~ Done, and the trap was already defused: the
  bearer-auth work in section 2 had to add explicit `System.IdentityModel.Tokens.Jwt` and
  `Microsoft.IdentityModel.Tokens` references (7.1.2) for its own reasons, so by the time this
  came round the package could be dropped safely. The unused
  `Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General` import went with it.

- [x] ~~**Raw `new HttpClient()` registered as scoped.**~~ Both registrations deleted, not one.
  The item flags the raw client and points at the named `AddHttpClient("ApiClient")` above it
  as the correct alternative — but that one has no consumer either. Nothing in the application
  injects `HttpClient` or `IHttpClientFactory`, and the MAUI client has its own. A comment
  where they stood records that.

- [x] ~~**Misleading `[Key]` attributes on join entities.**~~ Removed, and the dead columns
  dropped with them in `20260908013332_DropUnusedMemberColumns`. All three join entities now
  match `MemberServiceRole`, which always had the right shape.
  Removing the attribute alone would have needed no migration — EF would simply have kept
  mapping an ordinary always-zero column. Dropping the column is what needs one.

- [x] ~~**`DeactivatedUserPurgeService.StartupDelay`**~~ Now `UserRetention:StartupDelayMinutes`,
  beside the other retention settings.
  `DateTime.UtcNow` stays, deliberately. The five purge tests call `PurgeAsync` directly and
  set `DeactivatedDate` far in the past, so they never wait and never need a clock; a
  `TimeProvider` seam would buy an exact-boundary test and nothing else. `PurgeAsync` therefore
  stays `internal` and the skill note explaining why stays accurate.

- [x] ~~**Test factory removes all hosted services.**~~ Now removes the one descriptor whose
  implementation type is `DeactivatedUserPurgeService`, and asserts it found it — so a rename
  or a move fails loudly instead of silently removing nothing. Exactly one hosted service is
  registered today, so the blanket call was harmless; it would have stopped any service added
  later from running under every integration test, and a background job that never runs in
  tests fails quietly.

- [x] ~~**The MAUI project is not in `StewardshipSurvey.sln`.**~~ Left as it is; this is not a
  defect. CI runs on `ubuntu-latest`, which has no MAUI workloads, and `build.yml:19-20`
  already says so. Adding it would break CI to remove a local surprise. Recorded here so the
  next reader does not re-derive it — and noted in the MAUI README, which now states plainly
  that nothing builds or tests that project automatically.

- [x] ~~**`StewardshipSurvey.Maui/README.md` mojibake.**~~ Fixed. The file was already pure
  ASCII with no BOM: the box-drawing characters and check marks had been replaced by literal
  `?` bytes at some earlier save, so nothing could be recovered by re-encoding — the originals
  were gone.
  Redrawn with `+--` and `|` rather than restoring the Unicode, so the same encoding round-trip
  cannot break it again. The check-mark bullets became ordinary list items. Verified: the file
  contains no non-ASCII bytes and the only remaining `?` is a nullable type in a code sample.

- [x] ~~**`Pages/Index.cshtml` commented scripture sections.**~~ Kept and annotated, per your
  call. The note records what a future reader needs: it is planned content, and it cannot
  simply be uncommented — the block opens three comments and closes one, and the "How" section
  has a stray closing tag.
  Two things I had wrong while doing this, both now checked rather than assumed: Razor comments
  **do** nest, which is why three unmatched openings swallow the block harmlessly; and an
  unmatched *closing* marker is a build error (RZ1003), not a silent leak. So this cannot break
  quietly.
  `AnonymousAccessTests` gained a test that the placeholder text stays off the rendered page.
  Teeth verified by uncommenting the block. Note it does **not** assert on `introBlock` — the
  home page has a live element by that id, which is a trap I fell into first.

- [x] ~~**Commented-out `DbSet<ServiceRoles>`.**~~ Both deleted, per your call to treat this
  pair as dead rather than as a placeholder. `Models/ServiceRoles.cs` was commented out in its
  entirety — every line, including the namespace — and nothing referenced the type.

- [x] ~~**`AddRazorSupportForMvc=true`**~~ Removed. Build clean, 0 warnings, all tests pass.

---

## 5. Development database cleanup

Delivered as `scripts/dev-cleanup.sql` and `scripts/README.md` rather than run for you: these
are deletes against your development database, and the connection string is the only thing
standing between the script and the wrong one. It is written to be run twice - as committed it
ends in `ROLLBACK`, so the first run shows what would go and changes nothing.

- [x] ~~**Five leftover test accounts**~~ Deleted. All five had `MemberID` NULL, so no profile
  and no survey answers hung off them — the cascade the script is careful about turned out to
  have nothing to carry. 17 accounts down to 12.
- [x] ~~**`staff@stmark.local` is polluted**~~ Cleared. Profile 7 read "Test Staffer" with
  PrefersEmail set; name fields are now empty and all three contact preferences off. The
  account itself is untouched and still signs in.
- [x] ~~**Rotate `Jwt:Key`**~~ Rotated. 48 random bytes, base64, 64 characters — comfortably
  past the 32-byte HMAC-SHA256 minimum the application enforces. Generated inline and written
  straight to user secrets, so the value never appeared in a command line, a log or a
  transcript; the change was confirmed by comparing SHA-256 fingerprints rather than by
  printing it.
  Verified the application starts on the new key and that `/api` answers an anonymous call
  with 401 JSON rather than a redirect.
  **Not verified:** a successful sign-in minting a token with this key, which needs credentials
  for a real account. If the key were unusable the failure would be loud and immediate — a 500
  naming `Jwt:Key` on the first login — not silent.
  Every token issued before now is dead, which is the point of a rotation.

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
