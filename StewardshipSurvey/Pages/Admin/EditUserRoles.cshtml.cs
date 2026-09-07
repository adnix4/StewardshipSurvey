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
    public class EditUserRolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public EditUserRolesModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();

        /// <summary>Membership status of this user's profile; null when unanswered.</summary>
        public MembershipStatus? MembershipStatus { get; set; }

        /// <summary>False when the person has not started a profile, so there is nothing to set.</summary>
        public bool HasProfile { get; set; }

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

            await _userManager.AddToRolesAsync(user, rolesToAdd);
           
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Membership status lives on the profile; the roles mirror it.
            var profile = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

            if (profile != null && profile.MembershipStatus != membershipStatus)
            {
                profile.MembershipStatus = membershipStatus;
                profile.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            if (profile != null)
            {
                await MembershipStatusRoles.SyncAsync(_userManager, user, profile.MembershipStatus);
            }
          
            return RedirectToPage("/Admin/UserRoles");
        }
    }
}
