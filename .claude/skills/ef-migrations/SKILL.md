---
name: ef-migrations
description: Entity Framework model and migration conventions for the Stewardship Survey - where migrations live, how to add one, and the traps around keys and the test schema. Use when changing an entity, adding or reviewing a migration, or working out why the database and the model disagree.
---

# EF Core in the Stewardship Survey

Provider is SQL Server (LocalDB in development). The context is
`Data/ApplicationDbContext.cs`, deriving from `IdentityDbContext<ApplicationUser>`.

**Migrations live in `StewardshipSurvey/Data/Migrations/`, not `StewardshipSurvey/Migrations/`.**
That is not the default and it is the first thing a fresh session gets wrong.

## Adding a migration

From the `StewardshipSurvey` project folder:

```bash
dotnet ef migrations add <Name> --output-dir Data/Migrations
```

Omit `--output-dir` and you get a second, parallel migrations folder that EF will happily
ignore.

Then **read the generated file before committing it**, because nothing else will — see below.
Check:

- `Up` does what you meant, and nothing you did not mean.
- `Down` genuinely reverses it. EF is good at this but not infallible.
- If EF prints *"An operation was scaffolded that may result in the loss of data"*, work out
  whether that is true and record the answer in the migration's doc comment. It is often a
  false alarm — dropping a column that was always constant loses nothing — but the reader
  after you cannot tell that from the diff.

## The gap that matters most

**No test in this repository exercises a migration.** The test factory calls
`Database.EnsureCreated()`, which builds the SQLite schema straight from the EF model. So:

- A migration that is wrong in a way the model is right about will pass every test and CI.
- A migration that fails on SQL Server specifically — a type, a default, a constraint — will
  not be caught until someone runs it.

A migration is therefore reviewed, not proven. Say so when you write one.

## Fluent configuration silently overrides attributes

`OnModelCreating` wins. This produced a real defect:

```csharp
public class MemberInterest
{
    [Key]                                   // a lie
    public int MemberInterestID { get; set; }
}
```
```csharp
modelBuilder.Entity<MemberInterest>()
    .HasKey(mi => new { mi.MemberID, mi.InterestAreaID });   // this is the key
```

The attribute was ignored, so `MemberInterestID` was never the key and EF never wrote it — a
real SQL Server column holding 0 on every row, in two tables, for a year. Both are gone now
(`20260908013332_DropUnusedMemberColumns`) and all three join entities match
`MemberServiceRole`, which never had the problem.

**When the two disagree, believe `OnModelCreating`.** If you are adding a key or a
relationship, put it in one place and do not decorate the entity as well.

## A property nothing assigns is not a foreign key

`MemberInfo.ApplicationUserID` looked exactly like a foreign key to `AspNetUsers` and was not
one — nothing ever assigned it, every row held the empty string, and the real link is
`AspNetUsers.MemberID`, configured with `HasForeignKey<ApplicationUser>`. It got its name by
being *renamed* from `PhoneNumber` in an early migration.

Before trusting a name, grep for an assignment.

## The composite-key join tables

`MemberInterest`, `MemberInvolvement` and `MemberServiceRole` are all keyed
`(MemberID, AreaID)`. Consequences worth knowing:

- The survey pages replace the whole set rather than diffing, so a POST that repeats an id
  violates the primary key. They call `.Distinct()` for that reason.
- Foreign keys are enforced in the tests: SQLite runs with `Foreign Keys=True` and
  `DatabaseConstraintTests` proves it, including cascade behaviour at the database level rather
  than in EF's change tracker.

## Applying a migration locally

```bash
dotnet ef database update            # from the StewardshipSurvey project folder
```

The development connection string is a LocalDB instance in `appsettings.json`. It carries no
credentials, so it is safe in source.

## Do not

- Do not hand-edit a migration that has already been applied somewhere. Add another.
- Do not switch the tests to `Microsoft.EntityFrameworkCore.InMemory` to make schema problems
  go away — it ignores foreign keys and cascade deletes, so `DatabaseConstraintTests` would
  pass vacuously. See `.claude/skills/testing`.
