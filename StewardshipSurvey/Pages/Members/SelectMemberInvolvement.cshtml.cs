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

                await LoadInvolvementsAsync();

                SelectedInvolvementIds = await _context.MemberInvolvements
                    .Where(mi => mi.MemberID == user.MemberID.Value)
                    .Select(mi => mi.InvolvementAreaID)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} involvements for member {MemberId}, {Selected} already selected",
                    AllInvolvements.Count, user.MemberID.Value, SelectedInvolvementIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the involvements page");
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

                _logger.LogInformation("Saving {Count} involvements for member {MemberId}",
                    SelectedInvolvementIds.Count, memberId);

                var existing = _context.MemberInvolvements.Where(mi => mi.MemberID == memberId);
                _context.MemberInvolvements.RemoveRange(existing);

                // Distinct because the key is (MemberID, InvolvementAreaID): a post repeating
                // an id would otherwise fail on the primary key, which is not something the
                // member did wrong or could correct.
                foreach (var involvementId in SelectedInvolvementIds.Distinct())
                {
                    _context.MemberInvolvements.Add(new MemberInvolvement
                    {
                        MemberID = memberId,
                        InvolvementAreaID = involvementId,
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // One SaveChanges, so the removals and the additions are a single transaction.
                // A failure here leaves the member's stored answers exactly as they were.
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved involvements for member {MemberId}", memberId);

                // A prospective member skips the "currently serving" step and closes the loop.
                var status = await _context.MemberInfos
                    .Where(m => m.MemberID == memberId)
                    .Select(m => m.MembershipStatus)
                    .FirstOrDefaultAsync();

                return status == MembershipStatus.ProspectiveMember
                    ? RedirectToPage("/Members/MemberInfo")
                    : RedirectToPage("/Members/SelectMemberServiceRoles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving involvements");
                ModelState.AddModelError(string.Empty,
                    "We could not save your selections. Nothing was changed - please try again.");
            }

            // Redisplay what the member just ticked, not what is stored. Calling OnGetAsync
            // here - as this used to - overwrote their unsaved edit with the database's copy,
            // and threw away the IActionResult it returned, so an Unauthorized() became a 200.
            await ReloadInvolvementsAfterFailedSaveAsync();
            return Page();
        }

        private async Task LoadInvolvementsAsync()
        {
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
        }

        private async Task ReloadInvolvementsAfterFailedSaveAsync()
        {
            try
            {
                await LoadInvolvementsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reload the involvement list after a failed save");
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
            AllInvolvements.Clear();
            ModelState.AddModelError(string.Empty,
                "We could not load your ministries just now. Please try again in a moment.");
        }
    }
}
