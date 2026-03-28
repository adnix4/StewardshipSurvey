# API Authorization Fix - JSON Parsing Error

## Problem
You received this error:
```
Error loading interests: '<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.
```

This error occurs when the `JsonSerializer` tries to parse HTML instead of JSON. The `<` character is the start of an HTML tag.

## Root Cause
All API endpoints were marked with `[Authorize]` at the class level. When unauthenticated or improperly authenticated requests hit these endpoints, the framework redirects to the login page instead of returning JSON.

The response was HTML (the login page), not JSON, causing the deserialization to fail.

## Solution
Updated all API controllers to use granular authorization:

### Before (Class-level [Authorize])
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Applied to ALL methods
public class InterestsController : ControllerBase
```

### After (Method-level authorization)
```csharp
[ApiController]
[Route("api/[controller]")]
public class InterestsController : ControllerBase
{
    [HttpGet("all")]
    [AllowAnonymous]  // Public endpoint - no auth required
    public async Task<ActionResult<List<InterestAreaDto>>> GetAllInterests()

    [HttpGet("current")]
    [Authorize]  // Protected endpoint - requires auth
    public async Task<ActionResult<List<MemberInterestDto>>> GetCurrentMemberInterests()

    [HttpPost("current")]
    [Authorize]  // Protected endpoint - requires auth
    public async Task<ActionResult> UpdateCurrentMemberInterests([FromBody] List<int> interestAreaIds)
```

## Changes Made

### ServiceRolesController
- ? `GET /api/serviceroles/all` - `[AllowAnonymous]` 
- ? `GET /api/serviceroles/current` - `[Authorize]`
- ? `POST /api/serviceroles/current` - `[Authorize]`

### InterestsController
- ? `GET /api/interests/all` - `[AllowAnonymous]`
- ? `GET /api/interests/current` - `[Authorize]`
- ? `POST /api/interests/current` - `[Authorize]`

### InvolvementsController
- ? `GET /api/involvements/all` - `[AllowAnonymous]`
- ? `GET /api/involvements/current` - `[Authorize]`
- ? `POST /api/involvements/current` - `[Authorize]`

## Why This Works

1. **Public endpoints** (`/all`) - Anyone can fetch the list of available interests, service roles, and involvement areas
2. **Protected endpoints** (`/current`) - Only authenticated users can view or update their own selections
3. **Proper JSON responses** - Public endpoints return JSON instead of HTML redirects

## Build Status
? **Build Successful** - All code compiles without errors

## Testing
After restarting your application:
1. Navigate to `/Members/SelectInterests`
2. The page should now load interests from the API without errors
3. You should see all available interests displayed
4. You can select and save your choices

## API Security Summary

| Endpoint | Authentication | Purpose |
|----------|----------------|---------|
| GET /api/interests/all | None | Load available interests for selection |
| GET /interests/current | Required | Get current user's selected interests |
| POST /interests/current | Required | Save current user's interest selections |
| GET /api/serviceroles/all | None | Load available service roles |
| GET /api/serviceroles/current | Required | Get current user's service roles |
| POST /api/serviceroles/current | Required | Save current user's service roles |
| GET /api/involvements/all | None | Load available involvement areas |
| GET /api/involvements/current | Required | Get current user's involvements |
| POST /api/involvements/current | Required | Save current user's involvements |
