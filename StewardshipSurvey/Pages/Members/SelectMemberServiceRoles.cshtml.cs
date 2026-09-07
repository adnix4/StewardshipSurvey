using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Models.DTOs;

namespace StewardshipSurvey.Pages.Members
{
    [Authorize]
    public class SelectMemberServiceRolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SelectMemberServiceRolesModel> _logger;

        public SelectMemberServiceRolesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<SelectMemberServiceRolesModel> logger)
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
                _logger.LogInformation("Loading SelectMemberServiceRoles page");
                
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading service roles for user {user.Email} (MemberID: {user.MemberID})");

                // Load all service roles from database
                AllInvolvements = await _context.InvolvementAreas
                    .Where(ia => ia.IsActive)
                    .OrderBy(ia => ia.AreaOfInvolvement)
                    .Select(ia => new InvolvementAreaDto
                    {
                        InvolvementAreaID = ia.InvolvementAreaID,
                        AreaOfInvolvement = ia.AreaOfInvolvement,
                        Description = ia.Description ?? string.Empty,
                        IsActive = ia.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation($"Loaded {AllInvolvements.Count} service roles");

                // Load current user's selections from database
                SelectedInvolvementIds = await _context.MemberServiceRoles
                    .Where(msr => msr.MemberID == user.MemberID.Value)
                    .Select(msr => msr.InvolvementAreaID)
                    .ToListAsync();

                _logger.LogInformation($"User has {SelectedInvolvementIds.Count} previously selected service roles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading service roles: {ex.Message}");
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

                _logger.LogInformation($"Saving {SelectedInvolvementIds.Count} service roles for user {user.Email}");

                // Remove existing service roles
                var existing = _context.MemberServiceRoles.Where(msr => msr.MemberID == memberId);
                _context.MemberServiceRoles.RemoveRange(existing);

                // Add new selections
                foreach (var involvementId in SelectedInvolvementIds)
                {
                    _context.MemberServiceRoles.Add(new MemberServiceRole
                    {
                        MemberID = memberId,
                        InvolvementAreaID = involvementId,
                        CreatedDate = DateTime.UtcNow
                    });
                    _logger.LogInformation($"Added service role {involvementId}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Service roles saved successfully to database");

                return RedirectToPage("/Members/MemberInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving service roles: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving your selections.");
            }

            // Reload data on error
            await OnGetAsync();
            return Page();
        }
    }
}
