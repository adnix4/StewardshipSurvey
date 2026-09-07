// Scaffolded locally to replace the Identity UI default, which rendered a working
// confirmation link on screen for anyone who registered. That made email confirmation prove
// nothing - a registrant could confirm their own address - while advertising the
// misconfiguration to every visitor.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using StewardshipSurvey.Data;
using System.Text;

namespace StewardshipSurvey.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public RegisterConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _environment = environment;
        }

        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Development only. Outside Development the link is never rendered, so confirming an
        /// address requires actually receiving the message sent to it.
        /// </summary>
        public bool ShowConfirmationLink { get; set; }

        public string? ConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;
            ShowConfirmationLink = _environment.IsDevelopment();

            if (ShowConfirmationLink)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                ConfirmationUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);
            }

            return Page();
        }
    }
}
