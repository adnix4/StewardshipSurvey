# Web API Layer Implementation Summary

## Overview
Your application now has a complete Web API layer that separates business logic from the UI. This allows your Razor Pages, MAUI apps, and any other client to consume the same API endpoints.

## Architecture

```
Clients (Web, MAUI, etc.)
    ?
ASP.NET Core Web API (Controllers)
    ?
DTOs (Data Transfer Objects)
    ?
ApplicationDbContext (EF Core)
    ?
SQL Server Database
```

## API Endpoints

### ServiceRoles Controller (`/api/serviceroles`)

- **GET `/api/serviceroles/all`** - Get all active service roles (involvement areas)
- **GET `/api/serviceroles/current`** - Get current user's selected service roles
- **POST `/api/serviceroles/current`** - Update current user's service roles

### Interests Controller (`/api/interests`)

- **GET `/api/interests/all`** - Get all active interests
- **GET `/api/interests/current`** - Get current user's selected interests
- **POST `/api/interests/current`** - Update current user's interests

### Involvements Controller (`/api/involvements`)

- **GET `/api/involvements/all`** - Get all active involvement areas
- **GET `/api/involvements/current`** - Get current user's selected involvement areas
- **POST `/api/involvements/current`** - Update current user's involvement areas

### Members Controller (`/api/members`)

- **GET `/api/members/current`** - Get current user's member information
- **GET `/api/members/{id}`** - Get specific member's information
- **PUT `/api/members/current`** - Update current user's member information

### Reports Controller (`/api/reports`) - Staff/Admin Only

- **GET `/api/reports/members`** - Get member report with filtering and sorting
  - Query Parameters:
    - `searchTerm` - Search by name, email, or phone
    - `sortColumn` - Sort by FirstName, Email, or Phone
    - `sortAscending` - Sort direction (true/false)
    - `serviceRoles` - Comma-separated IDs of service roles to filter
    - `interests` - Comma-separated IDs of interests to filter
    - `involvementAreas` - Comma-separated IDs of involvement areas to filter

## Refactored Razor Pages

The following Razor Pages have been updated to use the API:

1. **SelectMemberServiceRoles.cshtml.cs** - Uses ServiceRoles API
2. **SelectInterests.cshtml.cs** - Uses Interests API
3. **SelectMemberInvolvement.cshtml.cs** - Uses Involvements API

These pages now make HTTP calls to the API instead of directly accessing the database.

## Data Transfer Objects (DTOs)

Located in: `TestUserLogIn/Models/DTOs/DTOs.cs`

- `MemberReportDto` - Member report data
- `MemberDto` - Member information
- `MemberServiceRoleDto` - Service role assignment
- `MemberInterestDto` - Interest assignment
- `MemberInvolvementDto` - Involvement assignment
- `InvolvementAreaDto` - Involvement area data
- `InterestAreaDto` - Interest area data

## Program Configuration

Updated `Program.cs` includes:

- **AddControllers()** - Adds API controller support
- **AddCors()** - Enables Cross-Origin Resource Sharing
- **AddHttpClient()** - Registers HttpClient for Razor Pages
- **MapControllers()** - Routes API endpoints

## Authorization

All API endpoints require authentication:
- Public endpoints: `[Authorize]` - Any logged-in user
- Admin endpoints: `[Authorize(Roles = "Staff,Admin")]` - Staff and Admin roles only

## Usage in MAUI (Future)

Once you create your MAUI app, you can call these endpoints:

```csharp
var httpClient = new HttpClient { BaseAddress = new Uri("https://your-api.com") };
var response = await httpClient.GetAsync("api/serviceroles/all");
var json = await response.Content.ReadAsStringAsync();
var roles = JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json);
```

## Benefits

? **Code Reuse** - Share business logic across multiple clients
? **Scalability** - API can handle multiple client types
? **Separation of Concerns** - Clean separation between UI and data access
? **Easy Testing** - API endpoints can be tested independently
? **Cross-Platform** - Support for web, mobile, and desktop
? **Future-Proof** - Ready for MAUI, mobile apps, or third-party integrations

## Next Steps

1. Test all API endpoints using Postman or Swagger
2. Update the MemberReport Razor Page to call the Reports API (optional)
3. Start building your MAUI app to consume these endpoints
4. Consider adding OpenAPI/Swagger documentation for better API documentation
