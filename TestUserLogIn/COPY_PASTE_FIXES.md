# Copy-Paste Code Fixes

## PRIMARY FIX: RESTART YOUR APPLICATION NOW

Before making any code changes, you MUST restart your application:
1. Shift+F5 (Stop Debugging)
2. Wait 5 seconds
3. F5 (Start Debugging)
4. Test your pages

The JSON errors will disappear once you restart.

---

## SECONDARY FIX: Preferred Contact Method (Single-Select)

### File: TestUserLogIn\Pages\Members\MemberInfo.cshtml

**FIND THIS CODE (around line 130-146):**
```razor
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
```razor
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

---

### File: TestUserLogIn\Pages\Members\MemberInfo.cshtml.cs

**FIND THIS CODE (around line 82-84 in the OnPost method):**
```csharp
                    existing.PrefersPhone = MemberDetails.PrefersPhone;
                    existing.PrefersEmail = MemberDetails.PrefersEmail;
                    existing.PrefersText = MemberDetails.PrefersText;
```

**REPLACE WITH THIS:**
```csharp
                    // Handle single-select preferred contact method
                    existing.PrefersPhone = false;
                    existing.PrefersText = false;
                    existing.PrefersEmail = false;

                    var form = HttpContext.Request.Form;
                    string preferredContact = form["PreferredContact"];
                    switch (preferredContact)
                    {
                        case "Phone":
                            existing.PrefersPhone = true;
                            break;
                        case "Text":
                            existing.PrefersText = true;
                            break;
                        case "Email":
                            existing.PrefersEmail = true;
                            break;
                    }
```

---

## Verify the Fixes

After making these changes:

1. Build Solution (Ctrl+Shift+B)
2. Restart application (F5)
3. Test the SelectInterests page - should load without errors
4. Test MemberInfo page - contact method should be single-select (radio buttons)

---

## Q&A

**Q: Will restarting the app delete my data?**
A: No. Restarting only restarts the application process. Your database is unchanged.

**Q: Do I need to clean and rebuild?**
A: Only the second fix (contact preferences) requires a rebuild. The API fix just requires a restart.

**Q: Why do I need to restart?**
A: The API code changes were made but the compiled code (what's actually running) is still the old version. Restarting forces it to recompile.

**Q: What if errors still happen after restart?**
A: 
1. Try: Build > Clean Solution
2. Then: Build > Rebuild Solution
3. Then: Stop and restart (F5)
4. Clear browser cache (Ctrl+Shift+Delete)
