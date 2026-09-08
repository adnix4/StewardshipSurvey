using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StewardshipSurvey.Data;
using StewardshipSurvey.Services;

namespace StewardshipSurvey.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccessTokenIssuer _tokens;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AccessTokenIssuer tokens,
            ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _tokens = tokens;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.Email) || string.IsNullOrEmpty(request?.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Same message and status as a wrong password, so the endpoint cannot be
                    // used to work out which addresses have accounts.
                    _logger.LogWarning("API login failed for {Email}: no such user.", request.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // CheckPasswordSignInAsync, not PasswordSignInAsync. The two do identical
                // lockout bookkeeping and return the same SignInResult; the difference is that
                // PasswordSignInAsync also issues the Identity cookie, so this endpoint used to
                // hand back a bearer token *and* start a cookie session. That left the API with
                // two authentication mechanisms while validating neither.
                //
                // lockoutOnFailure must stay true. It was false, which made this endpoint a
                // lockout bypass for the whole application: an attacker who found it could
                // guess passwords without limit even once the Razor login started counting.
                var result = await _signInManager.CheckPasswordSignInAsync(
                    user, request.Password, lockoutOnFailure: true);

                if (result.IsLockedOut)
                {
                    // Distinct from a plain rejection, which does tell a caller the account
                    // exists. Accepted deliberately: the Razor path already reveals as much by
                    // redirecting to its Lockout page, and leaving a locked-out member to
                    // retry a password that cannot work is the worse outcome.
                    _logger.LogWarning("API login blocked for {Email}: account locked out.", request.Email);
                    return StatusCode(StatusCodes.Status423Locked, new
                    {
                        message = "Too many failed attempts. This account is locked temporarily."
                    });
                }

                if (result.IsNotAllowed)
                {
                    // RequireConfirmedAccount is on. Reporting this as a bad password sent
                    // people round in circles retrying credentials that were already correct.
                    _logger.LogInformation("API login refused for {Email}: address not confirmed.", request.Email);
                    return Unauthorized(new
                    {
                        message = "You need to confirm your email address before signing in."
                    });
                }

                if (!result.Succeeded)
                {
                    _logger.LogWarning("API login failed for {Email}: invalid password.", request.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = await _tokens.IssueAsync(user);
                _logger.LogInformation("API login succeeded for {Email}.", request.Email);

                return Ok(new
                {
                    token = token,
                    email = user.Email,
                    message = "Login successful"
                });
            }
            catch (InvalidOperationException ex)
            {
                // Server misconfiguration (e.g. missing Jwt:Key). The message names a
                // configuration key, not a secret, so it is safe to return.
                _logger.LogError(ex, "JWT configuration error during login");
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}.", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Exchanges an access token for a fresh one. Anonymous by necessity: the caller's
        /// token has usually expired by the time they get here, so it cannot authenticate the
        /// request that renews it.
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
            {
                return BadRequest(new { message = "A token is required" });
            }

            try
            {
                // Signature, issuer and audience are all still enforced; only the expiry is
                // relaxed, and only as far as the configured refresh window.
                var principal = _tokens.ReadForRefresh(request.Token);
                if (principal == null)
                {
                    _logger.LogInformation("Token refresh refused: token unreadable or past the refresh window.");
                    return Unauthorized(new { message = "Please sign in again." });
                }

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = userId == null ? null : await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return Unauthorized(new { message = "Please sign in again." });
                }

                // Same stamp check the bearer handler applies, repeated here because this
                // endpoint deliberately bypasses that handler. Without it, refresh would be a
                // way for a revoked token to mint an unrevoked one.
                var stamp = principal.FindFirstValue(StewardshipClaims.SecurityStamp);
                if (stamp == null || stamp != await _userManager.GetSecurityStampAsync(user))
                {
                    _logger.LogInformation("Token refresh refused for {Email}: token superseded.", user.Email);
                    return Unauthorized(new { message = "Please sign in again." });
                }

                // A locked-out account must not be able to refresh its way through the lockout.
                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("Token refresh blocked for {Email}: account locked out.", user.Email);
                    return StatusCode(StatusCodes.Status423Locked, new
                    {
                        message = "Too many failed attempts. This account is locked temporarily."
                    });
                }

                var token = await _tokens.IssueAsync(user);
                _logger.LogInformation("Token refreshed for {Email}.", user.Email);

                return Ok(new { token = token, email = user.Email, message = "Token refreshed" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "JWT configuration error during refresh");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Clears the cookie, which matters for a browser caller.
            await _signInManager.SignOutAsync();

            // A bearer token cannot be withdrawn, so the only way to end an API session is to
            // move the security stamp the token was signed against. That invalidates every
            // outstanding token for this account on every device, not just the one calling -
            // a blunt instrument, and the only one a stateless token design offers.
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            return Ok(new { message = "Logout successful" });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
