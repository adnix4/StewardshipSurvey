---
name: razor-pages
description: Page-handler conventions for the Stewardship Survey - error handling, validation summaries, guarding POST as well as GET, and the traps that produced identical bugs on three pages. Use when writing or changing anything under Pages/, especially a page that saves a member's answers.
---

# Razor Pages in the Stewardship Survey

Pages live in `Pages/`, grouped by who can reach them: `Members/`, `Staff/`, `Admin/`, plus the
scaffolded Identity area under `Areas/Identity/`. Every page model carries an `[Authorize]`
appropriate to its folder.

The rules below are not style preferences. Each one is a bug that shipped.

## Never swallow an exception into a bare `return Page()`

Three pages — `SelectInterests`, `SelectMemberInvolvement`, `SelectMemberServiceRoles` — had
this shape:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "...");
}

return Page();          // looks like success
```

A member whose page failed to load got an empty form with nothing to say why. Worse, these
pages post a *full replacement* collection, so submitting that empty form deleted the answers
they had already given.

What to do instead:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error loading the interests page");
    RecordLoadFailure();     // sets LoadFailed, clears the options, adds a model error
}
```

and in the view:

```cshtml
@if (!Model.LoadFailed)
{
    <form method="post"> ... </form>
}
```

## A form that replaces a collection must not render empty

This is the rule the three pages above violated, and it is worth stating on its own because it
is not obvious: **an empty checkbox list posts exactly what "I unticked everything" posts.**
The server cannot tell them apart, so no amount of warning text makes an empty form safe. If
the options failed to load, do not render the form at all.

## Every page that adds a model error must render one

`ModelState.AddModelError` writes to something nothing displays unless the view asks for it.
All three survey pages called it and none rendered a summary, so the error was invisible even
on the paths that remembered to add one. The block this project uses
(`MemberInfo.cshtml`, `Admin/InterestAreas.cshtml`):

```cshtml
@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger" id="validationSummary">
        <ul class="mb-0">
            @foreach (var error in ViewData.ModelState.Values.SelectMany(v => v.Errors))
            {
                <li>@error.ErrorMessage</li>
            }
        </ul>
    </div>
}
```

## Never call `OnGetAsync()` from `OnPostAsync()`

It looks like a tidy way to redraw the page after a failed save. It does two wrong things:

1. **It discards the `IActionResult`.** `await OnGetAsync(); return Page();` turns an
   `Unauthorized()` into a 200.
2. **It overwrites what the member just submitted** with what is in the database, so a
   transient save failure looks as though their edit was thrown away.

Reload only the reference data the view needs and keep the posted values.

(`Admin/EditUser` still calls `await OnGetAsync(id)` inside `ApplyAsync` — that is the same
handler being used to repopulate a form, and it is deliberate there because the posted values
are route/form arguments rather than bound properties. Do not copy the pattern into a page with
`[BindProperty]` state.)

## Guard the POST wherever you guard the GET

`SelectMemberServiceRoles.OnGetAsync` redirected a prospective member away. `OnPostAsync` did
not, so the URL alone was enough to save answers to a step that does not apply to them. A
redirect on GET is navigation, not authorisation.

## Posting a replacement collection

The survey steps delete and re-add rather than diffing. Two things matter:

- **One `SaveChanges`.** The removals and the additions must be a single transaction, so a
  failure leaves the stored answers exactly as they were.
- **`.Distinct()` on the posted ids.** The join tables are keyed
  `(MemberID, AreaID)`, so a post repeating an id fails on the primary key — a failure the
  member neither caused nor could act on.

## Handler names

`OnGetExportAsync` is reachable as `?handler=Export`; the `On`/`Async` affixes are stripped.
Renaming a handler to add `Async` does not change its URL.

## Testing a page

See `.claude/skills/testing`. In short: every POST needs an antiforgery token pulled from a
rendered form, and a page that suppresses its form on failure has no token to pull — fetch one
from a page that does render a form.
