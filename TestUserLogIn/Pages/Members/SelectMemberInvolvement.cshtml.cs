using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestUserLogIn.Data;
using TestUserLogIn.Models;
using TestUserLogIn.Models.DTOs;

namespace TestUserLogIn.Pages.Members
{
    [Authorize]
    public class SelectMemberInvolvementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SelectMemberInvolvementModel> _logger;

        public SelectMemberInvolvementModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<SelectMemberInvolvementModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<InvolvementAreaDto> AllInvolvements { get; set; } = new();
        [BindProperty]
        public List<int> SelectedInvolvementIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading SelectMemberInvolvement page");
                
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading involvements for user {user.Email} (MemberID: {user.MemberID})");

                // Load all involvements from database
                AllInvolvements = await _context.InvolvementAreas
                    .Where(ia => ia.IsActive)
                    .OrderBy(ia => ia.AreaOfInvolvement)
                    .Select(ia => new InvolvementAreaDto
                    {
                        InvolvementAreaID = ia.InvolvementAreaID,
                        AreaOfInvolvement = ia.AreaOfInvolvement,
                        Description = ia.Description,
                        IsActive = ia.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation($"Loaded {AllInvolvements.Count} involvements");

                // Load current user's selections from database
                SelectedInvolvementIds = await _context.MemberInvolvements
                    .Where(mi => mi.MemberID == user.MemberID.Value)
                    .Select(mi => mi.InvolvementAreaID)
                    .ToListAsync();

                _logger.LogInformation($"User has {SelectedInvolvementIds.Count} previously selected involvements");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading involvements: {ex.Message}");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                int memberId = user.MemberID.Value;

                _logger.LogInformation($"Saving {SelectedInvolvementIds.Count} involvements for user {user.Email}");

                // Remove existing involvements
                var existing = _context.MemberInvolvements.Where(mi => mi.MemberID == memberId);
                _context.MemberInvolvements.RemoveRange(existing);

                // Add new selections
                foreach (var involvementId in SelectedInvolvementIds)
                {
                    _context.MemberInvolvements.Add(new MemberInvolvement
                    {
                        MemberID = memberId,
                        InvolvementAreaID = involvementId,
                        CreatedDate = DateTime.UtcNow
                    });
                    _logger.LogInformation($"Added involvement {involvementId}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Involvements saved successfully to database");

                return RedirectToPage("/Members/SelectMemberServiceRoles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving involvements: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving your selections.");
            }

            // Reload data on error
            await OnGetAsync();
            return Page();
        }
    }
}
