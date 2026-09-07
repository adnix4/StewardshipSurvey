using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StewardshipSurvey.Data;

namespace StewardshipSurvey.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
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

                // lockoutOnFailure must stay true. It was false, which made this endpoint a
                // lockout bypass for the whole application: an attacker who found it could
                // guess passwords without limit even once the Razor login started counting.
                var result = await _signInManager.PasswordSignInAsync(
                    user, request.Password, isPersistent: false, lockoutOnFailure: true);

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

                var token = GenerateJwtToken(user);
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

        private string GenerateJwtToken(ApplicationUser user)
        {
            // The signing key is a secret and has no fallback - a hardcoded default would
            // let anyone with the source mint valid tokens. Issuer and audience are not secrets.
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "Jwt:Key is not configured. From the StewardshipSurvey project folder, run: " +
                    "dotnet user-secrets set \"Jwt:Key\" \"<random value of at least 32 characters>\". " +
                    "See README.md.");
            }

            // HmacSha256 requires a key of at least 256 bits; a shorter one fails with an opaque IDX10603.
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key must be at least 32 bytes (256 bits) for HMAC-SHA256 signing.");
            }

            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "StewardshipSurvey";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "StewardshipSurveyUsers";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("MemberID", (user.MemberID ?? 0).ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logout successful" });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
