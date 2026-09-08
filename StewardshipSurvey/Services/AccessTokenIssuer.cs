using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StewardshipSurvey.Data;

namespace StewardshipSurvey.Services
{
    /// <summary>
    /// Mints and re-reads the API's bearer tokens. One place, so the claims a token carries and
    /// the claims the pipeline expects cannot drift apart.
    /// </summary>
    public sealed class AccessTokenIssuer
    {
        private readonly JwtOptions _options;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccessTokenIssuer(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager)
        {
            _options = options.Value;
            _userManager = userManager;
        }

        /// <summary>
        /// True when a usable signing key is configured. When it is false the bearer handler is
        /// left without a signing key, so every token is rejected, and this type throws a
        /// message naming the missing setting rather than the application failing to start.
        /// A developer who has not set the user secret gets a clear error on one endpoint
        /// instead of an app that will not run.
        /// </summary>
        public static bool IsConfigured(JwtOptions options) =>
            !string.IsNullOrWhiteSpace(options.Key)
            && Encoding.UTF8.GetByteCount(options.Key) >= JwtOptions.MinimumKeyBytes;

        /// <summary>
        /// Builds a signed token for <paramref name="user"/>, carrying their roles and the
        /// security stamp.
        /// </summary>
        /// <exception cref="InvalidOperationException">No usable signing key is configured.</exception>
        public async Task<string> IssueAsync(ApplicationUser user)
        {
            var credentials = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(StewardshipClaims.MemberId, (user.MemberID ?? 0).ToString()),

                // Without this the token cannot be revoked: it stays valid until it expires no
                // matter what happens to the account behind it.
                new(StewardshipClaims.SecurityStamp, await _userManager.GetSecurityStampAsync(user))
            };

            // Roles were missing entirely, so [Authorize(Roles = "Staff,Admin")] on
            // ReportsController and User.IsInRole in MembersController could never pass for a
            // bearer caller - the endpoints were unreachable rather than merely unauthorised.
            foreach (var role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Reads a token that may already have expired, for the refresh endpoint. The signature,
        /// issuer and audience must still be valid; the lifetime is checked separately against
        /// <see cref="JwtOptions.RefreshWindowMinutes"/> rather than rejected outright.
        /// <para>
        /// Returns null if the token is unreadable, was not signed by us, or expired longer ago
        /// than the refresh window allows.
        /// </para>
        /// </summary>
        public ClaimsPrincipal? ReadForRefresh(string token)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey(),

                // The whole point of this method: accept an expired token, then apply our own
                // window below. Everything else is still enforced.
                ValidateLifetime = false
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, parameters, out var validated);

                if (validated is not JwtSecurityToken jwt)
                {
                    return null;
                }

                // An unbounded window would make every token permanent, which is what this
                // change set exists to stop.
                if (jwt.ValidTo.AddMinutes(_options.RefreshWindowMinutes) < DateTime.UtcNow)
                {
                    return null;
                }

                return principal;
            }
            catch (Exception)
            {
                // A malformed or wrongly signed token is a normal thing for a public endpoint
                // to receive, not an exceptional one. The caller turns this into a 401.
                return null;
            }
        }

        private SymmetricSecurityKey SigningKey()
        {
            // No fallback key. A hardcoded default would let anyone holding the source mint
            // tokens this application accepts.
            if (string.IsNullOrWhiteSpace(_options.Key))
            {
                throw new InvalidOperationException(
                    "Jwt:Key is not configured. From the StewardshipSurvey project folder, run: " +
                    "dotnet user-secrets set \"Jwt:Key\" \"<random value of at least 32 characters>\". " +
                    "See README.md.");
            }

            if (Encoding.UTF8.GetByteCount(_options.Key) < JwtOptions.MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key must be at least {JwtOptions.MinimumKeyBytes} bytes " +
                    "(256 bits) for HMAC-SHA256 signing.");
            }

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        }
    }
}
