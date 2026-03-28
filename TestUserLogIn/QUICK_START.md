# QUICK START GUIDE - 3 Simple Steps

## STEP 1: RESTART YOUR APPLICATION (CRITICAL!)

### In Visual Studio:
```
1. Press Shift+F5  (Stop Debugging)
   ?? Wait for the console to say "Press any key to continue..."
   ?? Wait 5 more seconds

2. Close your web browser completely

3. Press F5  (Start Debugging)
   ?? Visual Studio will rebuild and restart
   ?? Browser will open automatically

4. Navigate to https://localhost:7001/Members/SelectInterests
   ?? The JSON errors should be GONE
```

**Why?** The API code was fixed, but the old compiled code is still running. Restarting forces recompilation.

---

## STEP 2: TEST THE FIXES

### Test SelectInterests Page:
```
? Page loads without errors
? Interests display properly
? Previously selected interests are checked
? Can select/unselect interests
? Click "Select Ministry Interests" button
? Redirects to next page
? Check database - data was saved
```

### Test SelectMemberInvolvement Page:
Same steps as above but for involvements

### Test SelectMemberServiceRoles Page:
Same steps as above but for service roles

---

## STEP 3: FIX PREFERRED CONTACT METHOD (Optional but Recommended)

### Only needed if you want radio buttons instead of checkboxes

**File 1: MemberInfo.cshtml**
- Find: `<div class="check-tile">`  (appears 3 times for contact methods)
- Replace with: `<div class="form-check">` and change inputs to `type="radio"`
- See COPY_PASTE_FIXES.md for exact code

**File 2: MemberInfo.cshtml.cs**
- Find: Lines that set `PrefersPhone`, `PrefersText`, `PrefersEmail`
- Replace with switch statement to handle single selection
- See COPY_PASTE_FIXES.md for exact code

---

## Expected Results After Restart

| What | Before | After |
|------|--------|-------|
| SelectInterests page | "Error loading interests: '<' is invalid" | Loads interests list |
| Previously selected | Nothing shows | Checkboxes are checked |
| Database updates | Don't work | Work correctly |
| Preferred contact | Multiple checkboxes | Single radio button |

---

## If Errors Persist After Restart

1. **Close everything:**
   - Close Visual Studio
   - Close browser
   - Wait 10 seconds

2. **Deep clean:**
   - In Visual Studio: Build > Clean Solution
   - Wait for it to finish
   - Build > Rebuild Solution
   - Wait for it to finish

3. **Restart:**
   - F5 to start debugging
   - Test the page again

4. **Check browser cache:**
   - Ctrl+Shift+Delete in browser
   - Clear all cached images and files
   - F5 to refresh page

5. **Last resort:**
   - Close Visual Studio completely
   - Delete `bin` and `obj` folders in your project
   - Reopen Visual Studio
   - F5 to start

---

## Success Indicators

### ? Everything is working if:
- SelectInterests page loads without errors
- Previously selected interests are shown as checked
- Can select/deselect interests
- Click save and database updates
- Preferred contact method uses radio buttons (after Step 3)

### ? Something is wrong if:
- Still seeing JSON parse errors
- Previously selected items don't show
- Database doesn't update after clicking save
- Still seeing multiple checkboxes for contact method

---

## Support

If you get stuck:
1. Check the logs in the Output window (View > Output)
2. Look for red error messages
3. Note the exact error message
4. Try the "If Errors Persist" steps above

Documents for reference:
- `COPY_PASTE_FIXES.md` - Exact code to copy/paste
- `ISSUES_AND_FIXES_SUMMARY.md` - Detailed explanation
- `COMPLETE_FIX_GUIDE.md` - In-depth troubleshooting
