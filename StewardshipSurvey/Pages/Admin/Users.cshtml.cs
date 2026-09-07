using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;


namespace StewardshipSurvey.Pages.Admin 
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly UserRetentionOptions _retention;

        public UsersModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IOptions<UserRetentionOptions> retention)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _retention = retention.Value;
        }

        public List<UserRoleViewModel> UsersWithRoles { get; set; } = new();

        public IEnumerable<UserRoleViewModel> ActiveUsers =>
            UsersWithRoles.Where(u => u.DeactivatedDate == null);

        public IEnumerable<UserRoleViewModel> DeactivatedUsers =>
            UsersWithRoles.Where(u => u.DeactivatedDate != null);

        public async Task OnGetAsync()
        {
            UsersWithRoles = new List<UserRoleViewModel>();
            var users =  await _userManager.Users
                .Include(u => u.Member) 
                .ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UsersWithRoles.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.Member?.FirstName ?? "No first name",
                    LastName = user.Member?.LastName ?? "No last name",
                    Roles = roles.ToList(),
                    DeactivatedDate = user.DeactivatedDate,
                    PurgeDate = user.DeactivatedDate == null || _retention.PurgeDeactivatedAfterDays <= 0
                        ? null
                        : user.DeactivatedDate.Value.AddDays(_retention.PurgeDeactivatedAfterDays),
                    RememberedRoles = string.IsNullOrWhiteSpace(user.DeactivatedRoles)
                        ? new List<string>()
                        : user.DeactivatedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                });
            }           
        }
        public class UserRoleViewModel
        {
            public string UserId { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new List<string>();

            /// <summary>Null while the account is active.</summary>
            public DateTime? DeactivatedDate { get; set; }

            /// <summary>When this account becomes eligible for permanent deletion.</summary>
            public DateTime? PurgeDate { get; set; }

            /// <summary>Elevated roles stripped at deactivation, offered back on reactivation.</summary>
            public List<string> RememberedRoles { get; set; } = new List<string>();
        }
              

        /// <summary>
        /// Restores a deactivated account. <paramref name="restoreRoles"/> carries the
        /// administrator's choice from the confirmation dialog; the decision is applied here
        /// rather than in the browser.
        /// </summary>
        public async Task<IActionResult> OnPostReactivateAsync(string id, bool restoreRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            if (restoreRoles && !string.IsNullOrWhiteSpace(user.DeactivatedRoles))
            {
                var remembered = user.DeactivatedRoles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(r => Roles.Elevated.Contains(r))
                    .ToList();

                if (remembered.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, remembered);
                }
            }

            user.DeactivatedDate = null;
            user.DeactivatedRoles = null;
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            var profile = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

            if (profile != null)
            {
                profile.IsActive = true;
                profile.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
