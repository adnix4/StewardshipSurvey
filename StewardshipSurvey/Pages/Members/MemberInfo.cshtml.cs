using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Models.DTOs;

namespace StewardshipSurvey.Pages
{
    [Authorize]
    public class MemberInfoModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MemberInfoModel> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public MemberInfoModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager,
            ILogger<MemberInfoModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Deliberately not the MemberInfo entity. See MemberProfileInput for what binding
        // the entity here allowed a crafted POST to reach.
        [BindProperty]
        public MemberProfileInput MemberDetails { get; set; } = new();

        [BindProperty]
        public string PreferredContact { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Load user roles
            Roles = await _userManager.GetRolesAsync(user);

            // Load existing MemberInfo if it exists
            var memberInfo = await _context.MemberInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

            if (memberInfo != null)
            {
                MemberDetails = MemberProfileInput.FromEntity(memberInfo);
                
                // Set the preferred contact based on the stored boolean values
                if (MemberDetails.PrefersPhone)
                    PreferredContact = "Phone";
                else if (MemberDetails.PrefersText)
                    PreferredContact = "Text";
                else if (MemberDetails.PrefersEmail)
                    PreferredContact = "Email";
                
                _logger.LogInformation("Loaded existing member with PreferredContact {PreferredContact}.", PreferredContact);
            }
            else
            {
                // The lookup above joins through the navigation property; this one follows
                // AspNetUsers.MemberID instead. They disagree for a profile whose link was
                // written from the user side, so both are tried before giving up.
                var byUserLink = user.MemberID == null
                    ? null
                    : await _context.MemberInfos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.MemberID == user.MemberID);

                MemberDetails = byUserLink != null
                    ? MemberProfileInput.FromEntity(byUserLink)
                    : new MemberProfileInput { FirstName = "", LastName = "", Email = user.Email };

                _logger.LogInformation("Started a new member profile.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                Roles = await _userManager.GetRolesAsync(user);
                return Page();
            }

            try
            {
                _logger.LogInformation("OnPost: PreferredContact value {PreferredContact}.", PreferredContact);
                
                // Reset all contact preferences
                MemberDetails.PrefersPhone = false;
                MemberDetails.PrefersEmail = false;
                MemberDetails.PrefersText = false;

                // Set the appropriate preference based on the radio button selection
                switch (PreferredContact)
                {
                    case "Phone":
                        MemberDetails.PrefersPhone = true;
                        _logger.LogInformation("Set PrefersPhone = true");
                        break;
                    case "Text":
                        MemberDetails.PrefersText = true;
                        _logger.LogInformation("Set PrefersText = true");
                        break;
                    case "Email":
                        MemberDetails.PrefersEmail = true;
                        _logger.LogInformation("Set PrefersEmail = true");
                        break;
                    default:
                        _logger.LogWarning("Unknown PreferredContact value {PreferredContact}.", PreferredContact);
                        break;
                }

                var existing = await _context.MemberInfos
                    .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

                if (existing == null)
                {
                    // Server-owned fields are set here, never taken from the request.
                    var created = new MemberInfo
                    {
                        ApplicationUser = user,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    MemberDetails.ApplyTo(created);

                    _context.MemberInfos.Add(created);
                    _logger.LogInformation("Created new MemberInfo for {User}", user.UserName);
                }
                else
                {
                    MemberDetails.ApplyTo(existing);
                    existing.UpdatedDate = DateTime.UtcNow;

                    _context.MemberInfos.Update(existing);
                    _logger.LogInformation("Updated MemberInfo for {User} - Phone: {Phone}, Text: {Text}, Email: {Email}", 
                        user.UserName, existing.PrefersPhone, existing.PrefersText, existing.PrefersEmail);
                }

                await _context.SaveChangesAsync();

                // Keep the Member / ProspectiveMember roles in step with the saved status.
                await MembershipStatusRoles.SyncAsync(_userManager, user, MemberDetails.MembershipStatus);

                // Refresh claims so FirstName and the status roles show up in the nav bar.
                await _signInManager.RefreshSignInAsync(user);

                _logger.LogInformation("MemberInfo saved successfully for {User}", user.UserName);

                return RedirectToPage("/Members/SelectInterests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving MemberInfo");
                ModelState.AddModelError(string.Empty, "There was a problem saving your information.");
                return Page();
            }
        }
    }
}
