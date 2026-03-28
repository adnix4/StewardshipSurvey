# HttpClient BaseAddress Fix - Summary

## Problem
When trying to call API endpoints from Razor Pages, you received the error:
```
An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.
```

## Root Cause
The `HttpClient` was being injected without a `BaseAddress` configured. When making relative URI calls like `"api/interests/all"`, the `HttpClient` needs to know the base URL of your application.

## Solution
Updated all three Razor Page models to:

1. **Inject `IHttpContextAccessor`** - Allows access to the current HTTP request
2. **Set BaseAddress dynamically** - Extracts the scheme and host from the current request
3. **Create `SetHttpClientBaseAddress()` method** - Called in both `OnGetAsync()` and `OnPostAsync()`

## Updated Files

### 1. SelectInterests.cshtml.cs
- Added `IHttpContextAccessor` to constructor
- Added `SetHttpClientBaseAddress()` method
- Called in both `OnGetAsync()` and `OnPostAsync()`

### 2. SelectMemberServiceRoles.cshtml.cs
- Same changes as SelectInterests

### 3. SelectMemberInvolvement.cshtml.cs
- Same changes as SelectInterests

### 4. Program.cs
- Added `builder.Services.AddHttpContextAccessor();`
- Created helper class `HttpClientWithBaseAddress` (optional, for future use)

## How It Works

```csharp
private void SetHttpClientBaseAddress()
{
    if (_httpClient.BaseAddress == null)
    {
        var request = _httpContextAccessor?.HttpContext?.Request;
        if (request != null)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }
}
```

This method:
- Checks if BaseAddress is already set (avoids overwriting)
- Gets the current HTTP request from `IHttpContextAccessor`
- Extracts scheme (http/https) and host (domain:port)
- Constructs the base URL and sets it on the `HttpClient`

## Example
If your application runs at `https://localhost:7001`:
- Base URL becomes: `https://localhost:7001`
- API call `api/interests/all` becomes: `https://localhost:7001/api/interests/all`

## Testing
1. Run your application
2. Navigate to the SelectInterests page
3. The page should now successfully load interests from the API
4. You should be able to select and save your choices

## Benefits
? No more "BaseAddress not set" errors
? Works in both Development and Production
? Automatically uses the correct scheme and host
? Minimal changes to existing code
? Clean, maintainable solution
