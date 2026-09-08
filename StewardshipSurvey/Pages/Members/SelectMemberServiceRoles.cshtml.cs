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
                // "Areas You Are Currently Serving In" does not apply to someone who is only
                // considering membership. The nav hides it for them; this is the real guard.
                if (await IsProspectiveMemberAsync())
                {
                    _logger.LogInformation("Prospective member redirected away from service roles");
                    return RedirectToPage("/Members/MemberInfo");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                await LoadServiceRolesAsync();

                SelectedInvolvementIds = await _context.MemberServiceRoles
                    .Where(msr => msr.MemberID == user.MemberID.Value)
                    .Select(msr => msr.InvolvementAreaID)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} service roles for member {MemberId}, {Selected} already selected",
                    AllInvolvements.Count, user.MemberID.Value, SelectedInvolvementIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the service roles page");
                RecordLoadFailure();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // The same guard OnGetAsync applies. It used to cover only the GET, so a
                // prospective member who knew the URL could POST straight past the redirect and
                // save answers to a step that does not apply to them. The helper's own doc
                // comment promised more than half a page.
                if (await IsProspectiveMemberAsync())
                {
                    _logger.LogInformation("Prospective member blocked from saving service roles");
                    return RedirectToPage("/Members/MemberInfo");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user?.MemberID == null)
                {
                    _logger.LogError("User not found or has no MemberID");
                    return Unauthorized();
                }

                int memberId = user.MemberID.Value;

                _logger.LogInformation("Saving {Count} service roles for member {MemberId}",
                    SelectedInvolvementIds.Count, memberId);

                var existing = _context.MemberServiceRoles.Where(msr => msr.MemberID == memberId);
                _context.MemberServiceRoles.RemoveRange(existing);

                // Distinct because the key is (MemberID, InvolvementAreaID): a post repeating
                // an id would otherwise fail on the primary key, which is not something the
                // member did wrong or could correct.
                foreach (var involvementId in SelectedInvolvementIds.Distinct())
                {
                    _context.MemberServiceRoles.Add(new MemberServiceRole
                    {
                        MemberID = memberId,
                        InvolvementAreaID = involvementId,
                        CreatedDate = DateTime.UtcNow
                    });
                }

                // One SaveChanges, so the removals and the additions are a single transaction.
                // A failure here leaves the member's stored answers exactly as they were.
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved service roles for member {MemberId}", memberId);

                return RedirectToPage("/Members/MemberInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service roles");
                ModelState.AddModelError(string.Empty,
                    "We could not save your selections. Nothing was changed - please try again.");
            }

            // Redisplay what the member just ticked, not what is stored. Calling OnGetAsync
            // here - as this used to - overwrote their unsaved edit with the database's copy,
            // and threw away the IActionResult it returned, so an Unauthorized() became a 200.
            await ReloadServiceRolesAfterFailedSaveAsync();
            return Page();
        }

        private async Task LoadServiceRolesAsync()
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

        private async Task ReloadServiceRolesAfterFailedSaveAsync()
        {
            try
            {
                await LoadServiceRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reload the service role list after a failed save");
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
                "We could not load your service roles just now. Please try again in a moment.");
        }

        /// <summary>
        /// True when the signed-in person has answered "Prospective Member" on their profile.
        /// Reads MemberInfo rather than the mirrored role so a stale sign-in cookie cannot
        /// grant access to a step the person should not see.
        /// </summary>
        private async Task<bool> IsProspectiveMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null) return false;

            var status = await _context.MemberInfos
                .Where(m => m.MemberID == user.MemberID)
                .Select(m => m.MembershipStatus)
                .FirstOrDefaultAsync();

            return status == MembershipStatus.ProspectiveMember;
        }
    }
}
