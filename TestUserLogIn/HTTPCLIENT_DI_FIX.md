# HttpClient Dependency Injection Fix

## Problem
You received this error:
```
System.InvalidOperationException: Unable to resolve service for type 'System.Net.Http.HttpClient' 
while attempting to activate 'TestUserLogIn.Pages.Members.SelectInterestsModel'.
```

## Root Cause
`HttpClient` was not properly registered in the dependency injection container in `Program.cs`.

## Solution
Updated `Program.cs` to properly register `HttpClient` as a scoped service:

```csharp
// Register HttpClient for dependency injection
builder.Services.AddScoped<HttpClient>(provider =>
{
    var httpClient = new HttpClient();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor?.HttpContext?.Request;
    
    if (request != null)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        httpClient.BaseAddress = new Uri(baseUrl);
    }
    
    return httpClient;
});
```

## How It Works

1. **AddScoped<HttpClient>()** - Registers HttpClient with scoped lifetime
2. **Factory function** - Creates a new HttpClient instance for each request
3. **BaseAddress setup** - Automatically sets the base URL from the current request

## What Changed
- ? Removed the unused `HttpClientWithBaseAddress` helper class
- ? Added proper HttpClient registration in DI container
- ? Maintains automatic BaseAddress configuration

## Next Steps
1. **Stop your running application** (if debugging)
2. **Restart the application** - The debugger needs to restart to pick up the registration changes
3. **Navigate to SelectInterests page** - Should now work without errors

## Build Status
? **Build Successful** - All code compiles without errors

## Testing
After restarting:
- Navigate to `/Members/SelectInterests`
- The page should load interests from the API
- You should be able to select and save interests without errors
