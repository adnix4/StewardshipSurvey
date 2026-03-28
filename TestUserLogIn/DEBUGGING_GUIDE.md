# SelectInterests Page - Debugging & Testing Guide

## What Was Fixed

Updated all three page models (`SelectInterests`, `SelectMemberInvolvement`, `SelectMemberServiceRoles`) with:
- ? **Comprehensive logging** - Log every step so you can debug issues
- ? **Better error messages** - See exactly what the API returns
- ? **Detailed status codes** - Know which API call failed

## How to Use the Logging to Debug

### Step 1: Restart Your Application
```
Shift+F5 (Stop)
Wait 5 seconds
F5 (Start)
```

### Step 2: Open the Output Window
In Visual Studio:
```
View > Output
```

Select **"Debug"** from the dropdown in the Output window.

### Step 3: Navigate to SelectInterests Page
```
https://localhost:7295/Members/SelectInterests
```

### Step 4: Watch the Output Window
You should see logs like:
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Set HttpClient BaseAddress to: https://localhost:7295

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Fetching all interests from api/interests/all

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Received interests: [{"interestAreaID":1,"interestArea":"Music","description":"...","isActive":true},...]

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Deserialized 5 interests

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Fetching current user's interests from api/interests/current

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Received current interests: [{"memberID":1,"interestAreaID":2,"interestArea":"Sports"}]

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  User has 1 selected interests
```

### Step 5: Click "Select Ministry Interests" Button
Watch the Output window for:
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  OnPost called with 3 selected interests

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Selected interest ID: 1

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Selected interest ID: 2

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Selected interest ID: 3

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Sending JSON: [1,2,3]

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  API Response: 200

TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Interests saved successfully
```

---

## Troubleshooting Guide

### Issue: See "Failed to fetch interests" Error

**In the Output window, you'll see:**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Failed to fetch interests: 404 - ...
```

**Solutions:**
1. Check if your API controller exists: `TestUserLogIn\Controllers\Api\InterestsController.cs`
2. Verify the route is correct: `api/interests/all`
3. Make sure the endpoint has `[AllowAnonymous]` on the `GetAllInterests()` method
4. Rebuild solution: `Ctrl+Shift+B`
5. Restart application: `Shift+F5`, wait, `F5`

---

### Issue: See "Failed to fetch current interests: 401"

**In the Output window, you'll see:**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Failed to fetch current interests: 401 - ...
```

**This means:** You're not authenticated to the API

**Solutions:**
1. Make sure you're logged in to the application
2. Verify the endpoint has `[Authorize]` on the `GetCurrentMemberInterests()` method
3. Check that your user has a valid MemberID in the database
4. Clear browser cookies and try again

---

### Issue: See "Failed to update interests: 401"

**In the Output window, you'll see:**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Failed to update interests: 401 - ...
```

**This means:** POST endpoint requires authentication

**Solutions:**
1. Verify `[Authorize]` is on the `UpdateCurrentMemberInterests()` POST method
2. Check that your user is properly authenticated
3. Look at the user's MemberID in the database

---

### Issue: No Interests Are Pre-Selected

**What you'll see:** Page loads but no checkboxes are checked, even if you previously selected some

**In the Output window, look for:**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  User has 0 selected interests
```

**Solutions:**
1. Check the `MemberInterests` table in your database - are there any rows for this user?
2. Verify `api/interests/current` is returning data:
   - In Output window, look for: `Received current interests: [...]`
   - If it shows `[]` (empty array), there are no interests saved for this user yet
3. The first time a user visits, they won't have any selections - that's normal

---

### Issue: Database Not Updating After Click

**What you'll see:** Click "Select Ministry Interests", page seems to process, but database doesn't change

**In the Output window, look for:**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  OnPost called with 3 selected interests
  ...
  API Response: 200
  Interests saved successfully
```

**If you see 200 and "saved successfully":**
- ? The API call succeeded
- Check the `MemberInterests` table in your database
- The data should be there

**If you see a different status code (like 500):**
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
  Failed to update interests: 500 - Internal Server Error
```
- The API encountered an error
- Check your API controller for bugs
- Check the browser DevTools > Network tab to see the full error response

---

## Testing Checklist

- [ ] Application is restarted (F5)
- [ ] Output window is open (View > Output)
- [ ] Navigate to SelectInterests page
- [ ] See logging in Output window
- [ ] Interests load and display
- [ ] Previously selected interests show as checked
- [ ] Can select/deselect interests
- [ ] Click "Select Ministry Interests" button
- [ ] See "API Response: 200" in Output
- [ ] Page redirects to next step
- [ ] Database was updated with new selections

---

## Quick Copy-Paste for Logs

If you're sharing logs with someone, copy from the Output window and paste the section that starts with:
```
TestUserLogIn.Pages.Members.SelectInterestsModel[0]
```

This helps identify exactly where the problem is.

---

## Expected Database State

After successfully selecting interests, check the `MemberInterests` table:

```
MemberID | InterestAreaID | CreatedDate
---------|----------------|--------------------
1        | 1              | 2024-12-19 10:30:00
1        | 2              | 2024-12-19 10:30:00
1        | 3              | 2024-12-19 10:30:00
```

If you see rows like this, the database update worked! ?
