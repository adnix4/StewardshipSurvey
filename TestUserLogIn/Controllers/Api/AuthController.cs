using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TestUserLogIn.Data;

namespace TestUserLogIn.Controllers.Api
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
                    _logger.LogWarning($"Login failed for {request.Email}: user not found");
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning($"Login failed for {request.Email}: invalid password");
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation($"User {request.Email} logged in successfully");

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
                _logger.LogError(ex, $"Error during login for {request.Email}");
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
                    "Jwt:Key is not configured. From the TestUserLogIn project folder, run: " +
                    "dotnet user-secrets set \"Jwt:Key\" \"<random value of at least 32 characters>\". " +
                    "See README.md.");
            }

            // HmacSha256 requires a key of at least 256 bits; a shorter one fails with an opaque IDX10603.
            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key must be at least 32 bytes (256 bits) for HMAC-SHA256 signing.");
            }

            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TestUserLogIn";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "TestUserLogInUsers";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName),
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
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
