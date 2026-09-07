using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StewardshipSurvey.Data;

namespace StewardshipSurvey.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserRolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditUserRolesModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();

        public class RoleSelection
        {
            public string ? RoleName { get; set; }
            public bool Selected { get; set; }
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

            var userRoles = await _userManager.GetRolesAsync(CurrentUser);
            foreach (var role in _roleManager.Roles)
            {
                Roles.Add(new RoleSelection
                {
                    RoleName = role.Name,
                    Selected = role.Name != null && userRoles.Contains(role.Name)
                });
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id, List<string> selectedRoles)
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

            await _userManager.AddToRolesAsync(user, rolesToAdd);
           
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
          
            return RedirectToPage("/Admin/UserRoles");
        }
    }
}
