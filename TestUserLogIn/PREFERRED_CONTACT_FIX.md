# Preferred Contact Method - Complete Fix

## Problem
The preferred contact method radio buttons were not:
1. Displaying the previously selected value
2. Saving the selection to the database

## Root Cause
The issue was in the HTML form binding. Using `asp-for="PreferredContact"` on radio buttons doesn't work properly because the tag helper overwrites the `value` attribute.

## Solution Implemented

### 1. **Hidden Input Field**
```html
<input type="hidden" name="PreferredContact" id="PreferredContactHidden" value="@Model.PreferredContact" />
```
- This holds the actual value that gets sent to the server
- Gets updated by JavaScript when a radio button is selected

### 2. **Visible Radio Button Group**
```html
<input class="form-check-input" type="radio" name="PreferredContactGroup" id="contactPhone" value="Phone" 
       @(Model.PreferredContact == "Phone" ? "checked" : "") />
```
- Uses a different `name` attribute (`PreferredContactGroup`)
- Checks if the current value matches to set the `checked` attribute
- This ensures previously selected values display correctly

### 3. **JavaScript Synchronization**
```javascript
document.querySelectorAll('input[name="PreferredContactGroup"]').forEach(radio => {
    radio.addEventListener('change', function () {
        document.getElementById('PreferredContactHidden').value = this.value;
        console.log('PreferredContact set to:', this.value);
    });
});
```
- When user clicks a radio button, the hidden input is updated
- This value gets sent to the server as `PreferredContact`

### 4. **Page Model Processing**
The MemberInfo.cshtml.cs already handles this correctly:

**On GET:**
```csharp
if (MemberDetails.PrefersPhone)
    PreferredContact = "Phone";
else if (MemberDetails.PrefersText)
    PreferredContact = "Text";
else if (MemberDetails.PrefersEmail)
    PreferredContact = "Email";
```

**On POST:**
```csharp
switch (PreferredContact)
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

## Data Flow

```
1. PAGE LOAD (OnGetAsync)
   ?
   Database MemberInfo retrieved
   ?
   PreferredContact property set based on PrefersPhone/PrefersText/PrefersEmail
   ?
   HTML displays correct radio button as checked

2. USER SELECTION
   ?
   User clicks a radio button
   ?
   JavaScript updates hidden input
   ?
   User clicks Save button

3. FORM SUBMISSION (OnPostAsync)
   ?
   Hidden input value "PreferredContact" is posted to server
   ?
   PageModel receives PreferredContact value
   ?
   Switch statement sets appropriate Prefers* boolean
   ?
   Data saved to database

4. NEXT PAGE LOAD
   ?
   Database shows the saved preference
   ?
   PreferredContact is set correctly
   ?
   Radio button displays as checked
```

## Testing Checklist

- [ ] Restart application (F5)
- [ ] Go to MemberInfo page
- [ ] Check Output window for logs showing PreferredContact value
- [ ] Select "Phone Call" radio button
- [ ] Click Save
- [ ] Check database: MemberInfos table should show PrefersPhone = 1, PrefersText = 0, PrefersEmail = 0
- [ ] Go back to MemberInfo page
- [ ] Verify "Phone Call" is still selected
- [ ] Select "Email" radio button
- [ ] Click Save
- [ ] Check database: PrefersPhone = 0, PrefersText = 0, PrefersEmail = 1
- [ ] Go back to MemberInfo page
- [ ] Verify "Email" is now selected

## Console Logging

When you select a radio button, you should see in the browser console:
```
PreferredContact set to: Phone
PreferredContact set to: Text
PreferredContact set to: Email
```

And in the application logs you'll see:
```
OnPost: PreferredContact value = 'Phone'
Set PrefersPhone = true
Updated MemberInfo for admin@yahoo.com - Phone: True, Text: False, Email: False
```

## Build Status
? **Build Successful** - Ready to test!
