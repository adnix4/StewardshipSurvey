# MemberReport InvalidCastException - FIXED

## The Problem

When filtering the member report by service roles, interests, or involvement areas, you got this error:

```
System.InvalidCastException: Unable to cast object of type 'Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryable`1[TestUserLogIn.Models.MemberInfo]' 
to type 'Microsoft.EntityFrameworkCore.Query.IIncludableQueryable`2[TestUserLogIn.Models.MemberInfo,TestUserLogIn.Models.InvolvementAreas]'.
```

## Root Cause

The code was trying to cast the result of `.Where()` to `IIncludableQueryable<MemberInfo, InvolvementAreas>`, which doesn't work because:

1. `.Where()` returns `IQueryable<T>`, not `IIncludableQueryable<T, TProperty>`
2. `IIncludableQueryable` is a special type that only exists immediately after `.Include()` or `.ThenInclude()`
3. Once you call `.Where()`, you lose the `IIncludableQueryable` type

### Wrong Code
```csharp
IQueryable<MemberInfo> query = _context.MemberInfos
    .Where(m => m.IsActive)
    .Include(m => m.MemberServiceRoles)
    .ThenInclude(msr => msr.InvolvementArea)
    .Include(m => m.MemberInterests)
    .ThenInclude(mi => mi.InterestArea)
    .Include(m => m.MemberInvolvements)
    .ThenInclude(mi => mi.InvolvementArea);

// ? WRONG - Trying to cast IQueryable to IIncludableQueryable
if (!string.IsNullOrEmpty(searchTerm))
{
    query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<MemberInfo, InvolvementAreas?>)query.Where(m =>
        m.FirstName.Contains(searchTerm) || ...);
}
```

## The Fix

Simply use `IQueryable<MemberInfo>` for all queries and don't try to cast:

### Correct Code
```csharp
// ? Use IQueryable<T>, not IIncludableQueryable
IQueryable<MemberInfo> query = _context.MemberInfos
    .Where(m => m.IsActive)
    .Include(m => m.MemberServiceRoles)
    .ThenInclude(msr => msr.InvolvementArea)
    .Include(m => m.MemberInterests)
    .ThenInclude(mi => mi.InterestArea)
    .Include(m => m.MemberInvolvements)
    .ThenInclude(mi => mi.InvolvementArea);

// ? Just assign directly, no casting needed
if (!string.IsNullOrEmpty(searchTerm))
{
    query = query.Where(m =>
        m.FirstName.Contains(searchTerm) || 
        m.LastName.Contains(searchTerm) || 
        m.Email.Contains(searchTerm) || 
        m.CellPhoneNumber.Contains(searchTerm));
}
```

## What Was Fixed

? Changed `OnGetAsync()` method:
- Removed all `IIncludableQueryable` casts
- Use `IQueryable<MemberInfo>` throughout
- Filters now apply correctly

? Changed `OnGetExport()` method:
- Removed all `IIncludableQueryable` casts
- Use `IQueryable<MemberInfo>` throughout
- Export functionality works with filters

## How It Works Now

```
1. Start with IQueryable including related data
   ?
2. Apply .Where() for search filter
   ?
3. Apply .Where() for service roles filter
   ?
4. Apply .Where() for interests filter
   ?
5. Apply .Where() for involvement areas filter
   ?
6. Apply .OrderBy() for sorting
   ?
7. Execute with .ToListAsync()
```

Each `.Where()` simply refines the IQueryable<MemberInfo> query without type casting.

## Testing

1. **Restart your application** (Shift+F5, wait, F5)
2. **Go to the Member Report page** (Staff/Admin only)
3. **Try filtering by:**
   - Service Roles - should work
   - Interests - should work
   - Involvement Areas - should work
4. **Try searching** by name/email/phone - should work
5. **Try exporting** to CSV with filters applied - should work

## Key Lesson

When working with Entity Framework Core queries:
- Use `IQueryable<T>` for general queries
- `IIncludableQueryable<T, TProperty>` is temporary and only exists right after `.Include()` or `.ThenInclude()`
- Never try to cast back to `IIncludableQueryable` after calling `.Where()`
- Always use `IQueryable<T>` as your query type when you'll be chaining multiple operations

## Build Status
? **Build Successful** - Ready to test!
