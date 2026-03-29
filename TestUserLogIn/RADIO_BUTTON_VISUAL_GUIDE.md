# Radio Button Implementation - Visual Guide

## How It Works Now

### Step 1: Page Loads
```
Database ? MemberInfo loaded
         ? PreferredContact = "Phone" (if PrefersPhone = true)
         ? HTML renders with correct radio button checked
```

### Step 2: User Interacts
```
User clicks "Email" radio button
         ?
JavaScript event fires
         ?
Hidden input updated: PreferredContact = "Email"
         ?
Form submitted
```

### Step 3: Server Processes
```
Server receives PreferredContact = "Email"
         ?
OnPost sets MemberDetails.PrefersEmail = true
         ?
Other Prefers* = false
         ?
Database updated
```

### Step 4: Verification
```
Go back to page
         ?
Database shows PrefersEmail = 1
         ?
PreferredContact = "Email"
         ?
"Email" radio button displays as checked
```

## HTML Structure

```html
<!-- Hidden field - gets the actual value -->
<input type="hidden" name="PreferredContact" id="PreferredContactHidden" 
       value="@Model.PreferredContact" />

<!-- Visible radio buttons - user interacts with these -->
<input type="radio" name="PreferredContactGroup" id="contactPhone" value="Phone" 
       @(Model.PreferredContact == "Phone" ? "checked" : "") />

<input type="radio" name="PreferredContactGroup" id="contactText" value="Text"
       @(Model.PreferredContact == "Text" ? "checked" : "") />

<input type="radio" name="PreferredContactGroup" id="contactEmail" value="Email"
       @(Model.PreferredContact == "Email" ? "checked" : "") />
```

## JavaScript Flow

```javascript
// When radio button changes:
1. Event listener detects change
2. Gets value from clicked radio button
3. Updates hidden input with that value
4. Logs to console (for debugging)

// When form is clicked:
1. Form-check div is clicked
2. Finds radio button inside
3. Checks the radio button
4. Triggers change event
5. Hidden input is updated
```

## Key Points

? **Previously selected values display correctly**
- OnGetAsync sets PreferredContact based on database booleans
- HTML uses PreferredContact to determine which radio is checked

? **New selections save to database**
- JavaScript syncs radio button to hidden input
- Hidden input posts PreferredContact value to server
- OnPostAsync processes the value and updates booleans
- SaveChangesAsync commits to database

? **No conflicts with model binding**
- Radio buttons use name="PreferredContactGroup"
- Hidden input uses name="PreferredContact"
- No tag helper conflicts

## Testing the Flow

1. **Clear browser cache** (Ctrl+Shift+Delete)
2. **Restart app** (Shift+F5, wait, F5)
3. **Open DevTools** (F12)
4. **Go to MemberInfo page**
5. **Check Console tab** - you'll see "PreferredContact set to: ..." when you click radio buttons
6. **Click Save**
7. **Check Output window** - you'll see the log messages from OnPostAsync
8. **Check Database** - verify the PrefersPhone/PrefersText/PrefersEmail columns
9. **Refresh page** - radio button should remain checked

## Expected Database Values

| Scenario | PrefersPhone | PrefersText | PrefersEmail |
|----------|--------------|-------------|--------------|
| Phone selected | 1 | 0 | 0 |
| Text selected | 0 | 1 | 0 |
| Email selected | 0 | 0 | 1 |
| None selected | 0 | 0 | 0 |
