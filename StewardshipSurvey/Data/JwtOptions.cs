namespace StewardshipSurvey.Data
{
    /// <summary>
    /// Signing and validation settings for the API's bearer tokens.
    /// Bound from the "Jwt" configuration section.
    /// <para>
    /// <see cref="Key"/> is a secret and never appears in a committed appsettings file: in
    /// development it comes from user secrets, in a deployment from the environment. Issuer
    /// and audience are not secrets and have defaults.
    /// </para>
    /// <para>
    /// Until this type existed the same three values were read as loose strings inside
    /// <c>AuthController</c> and nowhere else, because nothing validated tokens. Now that
    /// <c>Program.cs</c> validates what the controller signs, the two have to agree, and one
    /// options class is what makes them agree.
    /// </para>
    /// </summary>
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>
        /// HMAC-SHA256 needs at least 256 bits. A shorter key fails deep inside the token
        /// library with an opaque IDX10603, so it is worth checking up front.
        /// </summary>
        public const int MinimumKeyBytes = 32;

        public string? Key { get; set; }

        public string Issuer { get; set; } = "StewardshipSurvey";

        public string Audience { get; set; } = "StewardshipSurveyUsers";

        /// <summary>
        /// How long an access token is accepted for. Short on purpose: a token cannot be
        /// recalled once issued, so its lifetime is the window an attacker gets with a stolen
        /// one. The refresh endpoint is what makes a short lifetime workable for a client.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 60;

        /// <summary>
        /// How long after expiry a token may still be exchanged for a fresh one. Past this the
        /// member signs in again.
        /// <para>
        /// Seven days by default, which keeps the overall "stay signed in" window the same as
        /// the single seven-day token this replaced. What changed is that the credential on the
        /// wire is now good for an hour rather than a week, and every refresh re-checks the
        /// security stamp - so deactivating an account ends the session within the hour instead
        /// of never.
        /// </para>
        /// </summary>
        public int RefreshWindowMinutes { get; set; } = 60 * 24 * 7;
    }

    /// <summary>Claim types this application issues that are not in <see cref="System.Security.Claims.ClaimTypes"/>.</summary>
    public static class StewardshipClaims
    {
        /// <summary>The member's profile id, so an API caller need not look it up.</summary>
        public const string MemberId = "MemberID";

        /// <summary>
        /// The Identity security stamp as it stood when the token was issued. Compared against
        /// the stored stamp on every request, which is what makes a bearer token revocable:
        /// deactivating an account or changing its roles bumps the stamp and the token stops
        /// being accepted.
        /// </summary>
        public const string SecurityStamp = "sstamp";
    }

}
