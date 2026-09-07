using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;


namespace StewardshipSurvey.Pages.Admin 
{
    [Authorize(Roles = "Admin")]
    public class UserRolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<UserRoleViewModel> UsersWithRoles { get; set; }

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
                    Email = user.Email,
                    FirstName = user.Member?.FirstName ?? "No first name",
                    LastName = user.Member?.LastName ?? "No last name",
                    Roles = roles.ToList()
                });
            }           
        }
        public class UserRoleViewModel
        {
            public string UserId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public List<string> Roles { get; set; } = new List<string>();
        }
              
    }
}
