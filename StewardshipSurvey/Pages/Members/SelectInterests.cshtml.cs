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
    public class SelectInterestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SelectInterestsModel> _logger;

        public SelectInterestsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<SelectInterestsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<InterestAreaDto> AllInterests { get; set; } = new();
        [BindProperty]
        public List<int> SelectedInterestIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading SelectInterests page");
                
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading interests for user {user.Email} (MemberID: {user.MemberID})");

                // Load all interests from database
                AllInterests = await _context.InterestAreas
                    .Where(ia => ia.IsActive)
                    .OrderBy(ia => ia.InterestArea)
                    .Select(ia => new InterestAreaDto
                    {
                        InterestAreaID = ia.InterestAreaID,
                        InterestArea = ia.InterestArea,
                        Description = ia.Description,
                        IsActive = ia.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation($"Loaded {AllInterests.Count} interests");

                // Load current user's selections from database
                SelectedInterestIds = await _context.MemberInterests
                    .Where(mi => mi.MemberID == user.MemberID.Value)
                    .Select(mi => mi.InterestAreaID)
                    .ToListAsync();

                _logger.LogInformation($"User has {SelectedInterestIds.Count} previously selected interests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading interests: {ex.Message}");
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

                _logger.LogInformation($"Saving {SelectedInterestIds.Count} interests for user {user.Email}");

                // Remove existing interests
                var existing = _context.MemberInterests.Where(mi => mi.MemberID == memberId);
                _context.MemberInterests.RemoveRange(existing);

                // Add new selections
                foreach (var interestId in SelectedInterestIds)
                {
                    _context.MemberInterests.Add(new MemberInterest
                    {
                        MemberID = memberId,
                        InterestAreaID = interestId,
                        CreatedDate = DateTime.UtcNow
                    });
                    _logger.LogInformation($"Added interest {interestId}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Interests saved successfully to database");

                return RedirectToPage("/Members/SelectMemberInvolvement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving interests: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while saving your selections.");
            }

            // Reload data on error
            await OnGetAsync();
            return Page();
        }
    }
}
