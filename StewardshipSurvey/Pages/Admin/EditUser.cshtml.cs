using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditUserModel> _logger;

        public EditUserModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<EditUserModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();

        /// <summary>Membership status of this user's profile; null when unanswered.</summary>
        public MembershipStatus? MembershipStatus { get; set; }

        /// <summary>False when the person has not started a profile, so there is nothing to set.</summary>
        public bool HasProfile { get; set; }

        /// <summary>True when this account already holds Admin, which requires typed confirmation.</summary>
        public bool TargetIsAdmin { get; set; }

        /// <summary>True when deactivating this account is blocked outright.</summary>
        public bool CannotDeactivate { get; set; }

        /// <summary>Why deactivation is blocked, shown in place of the button.</summary>
        public string? CannotDeactivateReason { get; set; }

        public class RoleSelection
        {
            public string ? RoleName { get; set; }
            public bool Selected { get; set; }

            // RegisteredUser is held by every account, so it is shown but not editable.
            public bool Locked { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("User ID is required.");
            }
            CurrentUser = await _userManager.FindByIdAsync(id);
            if (CurrentUser == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            var profile = await _context.MemberInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == CurrentUser.Id);
            HasProfile = profile != null;
            MembershipStatus = profile?.MembershipStatus;

            var userRoles = await _userManager.GetRolesAsync(CurrentUser);

            TargetIsAdmin = userRoles.Contains(Data.Roles.Admin);
            CannotDeactivateReason = await DeactivationBlockedReasonAsync(CurrentUser);
            CannotDeactivate = CannotDeactivateReason != null;
            foreach (var role in _roleManager.Roles)
            {
                Roles.Add(new RoleSelection
                {
                    RoleName = role.Name,
                    Selected = role.Name == Data.Roles.RegisteredUser
                        || (role.Name != null && userRoles.Contains(role.Name)),
                    Locked = role.Name == Data.Roles.RegisteredUser
                        || (role.Name != null && Data.Roles.StatusMirrored.Contains(role.Name))
                });
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id, List<string> selectedRoles, MembershipStatus? membershipStatus)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            selectedRoles ??= new List<string>();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId && rolesToRemove.Contains("Admin"))
            {
                ModelState.AddModelError(string.Empty, "You cannot remove the Admin role from your own account.");
                await OnGetAsync(id);
                return Page();
            }

            // RegisteredUser is a catch-all: it can never be removed, and is restored if
            // missing. The disabled checkbox posts nothing, so this is what enforces it.
            rolesToRemove.Remove(Data.Roles.RegisteredUser);
            if (!currentRoles.Contains(Data.Roles.RegisteredUser)
                && !rolesToAdd.Contains(Data.Roles.RegisteredUser))
            {
                rolesToAdd.Add(Data.Roles.RegisteredUser);
            }

            // Member / ProspectiveMember follow the status radio below, never the checkboxes.
            rolesToAdd.RemoveAll(r => Data.Roles.StatusMirrored.Contains(r));
            rolesToRemove.RemoveAll(r => Data.Roles.StatusMirrored.Contains(r));

            // Both results were discarded and the page redirected as success regardless. A
            // concurrency stamp mismatch or a validation failure left the admin looking at a
            // "saved" screen with nothing saved.
            if (!await ApplyAsync(() => _userManager.AddToRolesAsync(user, rolesToAdd), id)
                || !await ApplyAsync(() => _userManager.RemoveFromRolesAsync(user, rolesToRemove), id))
            {
                return Page();
            }

            // Membership status lives on the profile; the roles mirror it.
            var profile = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

            var statusChanged = profile != null && profile.MembershipStatus != membershipStatus;

            if (statusChanged)
            {
                profile!.MembershipStatus = membershipStatus;
                profile.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            if (profile != null)
            {
                await MembershipStatusRoles.SyncAsync(_userManager, user, profile.MembershipStatus);
            }

            // Role claims live in the signed-in cookie and in any bearer token already issued,
            // so changing the role store alone left the target seeing their old menu until they
            // signed in again - and left an outstanding API token carrying the old roles until
            // it expired. Moving the security stamp ends both. Deactivation has always done
            // this (OnPostDeactivateAsync); the ordinary role change did not.
            if (rolesToAdd.Count > 0 || rolesToRemove.Count > 0 || statusChanged)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            return RedirectToPage("/Admin/Users");
        }

        /// <summary>
        /// Runs an Identity operation and reports a failure instead of redirecting as success.
        /// <para>
        /// Four call sites in this file threw the <see cref="IdentityResult"/> away and carried
        /// straight on to the redirect, so a concurrency-stamp mismatch or a validation error
        /// showed the admin a saved screen with nothing saved.
        /// <c>DeactivatedUserPurgeService</c> has always checked its results; this brings the
        /// page into line.
        /// </para>
        /// </summary>
        /// <returns>True when the operation succeeded. On failure the caller returns Page().</returns>
        private async Task<bool> ApplyAsync(Func<Task<IdentityResult>> operation, string id)
        {
            var result = await operation();

            if (result.Succeeded)
            {
                return true;
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogError("Identity operation failed for user {UserId}: {Errors}", id, errors);
            ModelState.AddModelError(string.Empty, $"The change could not be saved: {errors}");

            // Repopulate the form, or the admin gets an error above an empty page.
            await OnGetAsync(id);
            return false;
        }

        /// <summary>
        /// Why this account may not be deactivated, or null when it may be. Deactivating the
        /// account you are signed in as, or the last usable Admin, would lock the site's
        /// administration away from everyone.
        /// </summary>
        internal async Task<string?> DeactivationBlockedReasonAsync(ApplicationUser user)
        {
            if (user.DeactivatedDate != null)
            {
                return "This account is already deactivated.";
            }

            if (user.Id == _userManager.GetUserId(User))
            {
                return "You cannot deactivate your own account.";
            }

            if (await _userManager.IsInRoleAsync(user, Data.Roles.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(Data.Roles.Admin);

                // A deactivated admin must not keep the seat warm. Deactivation strips the
                // elevated roles, so this filter only matters for rows predating that.
                var activeAdmins = admins.Count(a => a.DeactivatedDate == null);
                if (activeAdmins <= 1)
                {
                    return $"This is the only active {Data.Roles.Admin} account.";
                }
            }

            return null;
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string id, string? confirmEmail)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            var blocked = await DeactivationBlockedReasonAsync(user);
            if (blocked != null)
            {
                ModelState.AddModelError(string.Empty, blocked);
                await OnGetAsync(id);
                return Page();
            }

            // Typed confirmation for an Admin account. Checked here, not only in the browser,
            // or the safeguard is decorative.
            if (await _userManager.IsInRoleAsync(user, Data.Roles.Admin)
                && !string.Equals(confirmEmail?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty,
                    $"Type {user.Email} exactly to confirm deactivating an {Data.Roles.Admin} account.");
                await OnGetAsync(id);
                return Page();
            }

            // Remember the elevated roles so reactivation can offer them back, then remove
            // them. RegisteredUser and the status-mirrored role stay: the first is the
            // catch-all every account holds, and removing the second would put the mirror out
            // of step with the profile's membership status.
            var currentRoles = await _userManager.GetRolesAsync(user);
            var elevated = currentRoles.Where(r => Data.Roles.Elevated.Contains(r)).ToList();

            if (elevated.Count > 0
                && !await ApplyAsync(() => _userManager.RemoveFromRolesAsync(user, elevated), id))
            {
                return Page();
            }

            user.DeactivatedRoles = string.Join(",", elevated);
            user.DeactivatedDate = DateTime.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            // Discarding this one was the worst of the four: a failure here means the account
            // was never actually locked out, while the screen reported it deactivated.
            if (!await ApplyAsync(() => _userManager.UpdateAsync(user), id))
            {
                return Page();
            }

            // Invalidate any session this person currently has open.
            await _userManager.UpdateSecurityStampAsync(user);

            var profile = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

            if (profile != null)
            {
                profile.IsActive = false;
                profile.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Admin/Users");
        }
    }
}
