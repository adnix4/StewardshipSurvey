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

        /// <summary>
        /// True when the page could not be loaded. The view hides the form while it is set: an
        /// empty checkbox list is indistinguishable from "I unticked everything", so letting
        /// the member submit one would delete the answers they had already given.
        /// </summary>
        public bool LoadFailed { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                await LoadInterestsAsync();

                SelectedInterestIds = await _context.MemberInterests
                    .Where(mi => mi.MemberID == user.MemberID.Value)
                    .Select(mi => mi.InterestAreaID)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} interests for member {MemberId}, {Selected} already selected",
                    AllInterests.Count, user.MemberID.Value, SelectedInterestIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the interests page");
                RecordLoadFailure();
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

                _logger.LogInformation("Saving {Count} interests for member {MemberId}",
                    SelectedInterestIds.Count, memberId);

                var existing = _context.MemberInterests.Where(mi => mi.MemberID == memberId);
                _context.MemberInterests.RemoveRange(existing);

                // Distinct because the key is (MemberID, InterestAreaID): a post repeating an
                // id would otherwise fail on the primary key, which is not something the
                // member did wrong or could correct.
                foreach (var interestId in SelectedInterestIds.Distinct())
                {
                    _context.MemberInterests.Add(new MemberInterest
                    {
                        MemberID = memberId,
                        InterestAreaID = interestId,
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // One SaveChanges, so the removals and the additions are a single transaction.
                // A failure here leaves the member's stored answers exactly as they were.
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved interests for member {MemberId}", memberId);

                return RedirectToPage("/Members/SelectMemberInvolvement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving interests");
                ModelState.AddModelError(string.Empty,
                    "We could not save your selections. Nothing was changed - please try again.");
            }

            // Redisplay what the member just ticked, not what is stored. Calling OnGetAsync
            // here - as this used to - overwrote their unsaved edit with the database's copy,
            // and threw away the IActionResult it returned, so an Unauthorized() became a 200.
            await ReloadInterestsAfterFailedSaveAsync();
            return Page();
        }

        private async Task LoadInterestsAsync()
        {
            AllInterests = await _context.InterestAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.InterestArea)
                .Select(ia => new InterestAreaDto
                {
                    InterestAreaID = ia.InterestAreaID,
                    InterestArea = ia.InterestArea,
                    Description = ia.Description ?? string.Empty,
                    IsActive = ia.IsActive
                })
                .ToListAsync();
        }

        private async Task ReloadInterestsAfterFailedSaveAsync()
        {
            try
            {
                await LoadInterestsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reload the interest list after a failed save");
                RecordLoadFailure();
            }
        }

        /// <summary>
        /// Puts the page into its unusable state: an explanation the member can actually see,
        /// and an empty option list so the view suppresses the form.
        /// </summary>
        private void RecordLoadFailure()
        {
            LoadFailed = true;
            AllInterests.Clear();
            ModelState.AddModelError(string.Empty,
                "We could not load your interests just now. Please try again in a moment.");
        }
    }
}
