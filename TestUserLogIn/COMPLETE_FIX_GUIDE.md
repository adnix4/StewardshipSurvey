# Complete Fix Guide - API Errors & Database Updates

## Issue #1: Still Getting JSON Parse Errors

### Root Cause
You changed the API code but haven't restarted your application. The old compiled code is still running.

### Solution
**CRITICAL: You must COMPLETELY STOP and RESTART your application**

1. In Visual Studio:
   - Click the **Stop Debugging** button (or press Shift+F5)
   - Wait 5 seconds for the process to fully stop
   - Close the browser
   - Press F5 to start debugging again

2. Navigate to your pages again:
   - `/Members/SelectInterests`
   - `/Members/SelectMemberInvolvement`
   - `/Members/SelectMemberServiceRoles`

The errors should disappear once the application restarts with the updated API controllers.

---

## Issue #2: Previously Selected Data Not Displaying

The page loads but doesn't show previously selected items.

### Why This Happens
The `OnGetAsync()` method loads the data from the API, but if the API call fails (due to the JSON error above), the lists are empty.

### Solution
Once you restart the application (Issue #1), this should be fixed automatically. The API will return proper JSON and the page will display previously selected items.

---

## Issue #3: Pages Not Updating the Database

The pages don't save changes to the database.

### Why This Happens
Same root cause - the JSON parsing errors are preventing the POST requests from working.

### How to Verify It's Fixed
1. Restart your application
2. Go to `/Members/SelectInterests`
3. Select some interests
4. Click "Select Ministry Interests"
5. Check the database to verify the `MemberInterests` table was updated

---

## Issue #4: Preferred Contact Method Should Be Single-Select

Currently using checkboxes (allows multiple), should be single-select.

### Solution
In `TestUserLogIn\Pages\Members\MemberInfo.cshtml`, replace the "Preferred Contact Methods" section:

**FIND THIS:**
```html
<!-- Contact Preferences -->
<div class="mb-3">
    <label class="form-label fw-semibold">Preferred Contact Methods</label>

    <div class="check-tile @(Model.MemberDetails.PrefersPhone ? "selected" : "")">
        <input asp-for="MemberDetails.PrefersPhone" type="checkbox" class="form-check-input" />
        <label class="form-check-label">Phone Call</label>
    </div>

    <div class="check-tile @(Model.MemberDetails.PrefersText ? "selected" : "")">
        <input asp-for="MemberDetails.PrefersText" type="checkbox" class="form-check-input" />
        <label class="form-check-label">Text Message</label>
    </div>

    <div class="check-tile @(Model.MemberDetails.PrefersEmail ? "selected" : "")">
        <input asp-for="MemberDetails.PrefersEmail" type="checkbox" class="form-check-input" />
        <label class="form-check-label">Email</label>
    </div>
</div>
```

**REPLACE WITH THIS:**
```html
<!-- Contact Preferences -->
<div class="mb-3">
    <label class="form-label fw-semibold">Preferred Contact Method</label>
    <p class="text-muted small">Select one preferred method of contact</p>

    <div class="form-check">
        <input class="form-check-input" type="radio" name="PreferredContact" id="contactPhone" value="Phone" 
               @(Model.MemberDetails.PrefersPhone && !Model.MemberDetails.PrefersText && !Model.MemberDetails.PrefersEmail ? "checked" : "") />
        <label class="form-check-label" for="contactPhone">
            <i class="bi bi-telephone"></i> Phone Call
        </label>
    </div>

    <div class="form-check">
        <input class="form-check-input" type="radio" name="PreferredContact" id="contactText" value="Text"
               @(Model.MemberDetails.PrefersText && !Model.MemberDetails.PrefersPhone && !Model.MemberDetails.PrefersEmail ? "checked" : "") />
        <label class="form-check-label" for="contactText">
            <i class="bi bi-chat-dots"></i> Text Message
        </label>
    </div>

    <div class="form-check">
        <input class="form-check-input" type="radio" name="PreferredContact" id="contactEmail" value="Email"
               @(Model.MemberDetails.PrefersEmail && !Model.MemberDetails.PrefersPhone && !Model.MemberDetails.PrefersText ? "checked" : "") />
        <label class="form-check-label" for="contactEmail">
            <i class="bi bi-envelope"></i> Email
        </label>
    </div>
</div>
```

### Update the Page Model Handler
In `TestUserLogIn\Pages\Members\MemberInfo.cshtml.cs`, update the `OnPost` method to handle the single-select:

**FIND THIS CODE (in the OnPost method where you save MemberDetails):**
```csharp
MemberDetails.PrefersPhone = form.ContainsKey("MemberDetails.PrefersPhone");
MemberDetails.PrefersText = form.ContainsKey("MemberDetails.PrefersText");
MemberDetails.PrefersEmail = form.ContainsKey("MemberDetails.PrefersEmail");
```

**REPLACE WITH THIS:**
```csharp
// Reset all contact preferences first
MemberDetails.PrefersPhone = false;
MemberDetails.PrefersText = false;
MemberDetails.PrefersEmail = false;

// Set the selected one
string preferredContact = form["PreferredContact"];
switch (preferredContact)
{
    case "Phone":
        MemberDetails.PrefersPhone = true;
        break;
    case "Text":
        MemberDetails.PrefersText = true;
        break;
    case "Email":
        MemberDetails.PrefersEmail = true;
        break;
}
```

---

## Step-by-Step Fix Summary

1. **Restart Application** ? DO THIS FIRST
   - Stop debugging (Shift+F5)
   - Wait 5 seconds
   - Start debugging (F5)
   - Test the SelectInterests page

2. **If errors are gone**, the database updates should work automatically

3. **Fix Preferred Contact Method** (if you want single-select)
   - Edit MemberInfo.cshtml (replace checkboxes with radio buttons)
   - Edit MemberInfo.cshtml.cs (update OnPost handler)

4. **Verify Everything Works**
   - Navigate to `/Members/SelectInterests`
   - Select interests
   - Click save
   - Verify in database that data was saved
   - Go back to page and verify selections appear

---

## Debugging Tips

If you still get errors after restarting:

1. **Clear browser cache** (Ctrl+Shift+Delete)
2. **Check if API endpoints are returning JSON:**
   - Open browser DevTools (F12)
   - Go to Network tab
   - Navigate to SelectInterests page
   - Look for requests to `api/interests/all`
   - Click on the request
   - Check the Response tab - should show JSON array, not HTML

3. **If Response shows HTML:**
   - API changes weren't picked up
   - Try: Build > Clean Solution, then Build > Rebuild Solution
   - Fully restart the application again

---

## Files to Modify

- ? `TestUserLogIn\Pages\Members\MemberInfo.cshtml` - Replace contact preferences section
- ? `TestUserLogIn\Pages\Members\MemberInfo.cshtml.cs` - Update OnPost handler

**No API code changes needed** - the changes were already made in the previous fix.
