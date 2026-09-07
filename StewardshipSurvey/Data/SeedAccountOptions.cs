namespace StewardshipSurvey.Data
{
    /// <summary>
    /// Credentials for a development seed account, supplied via configuration
    /// (user secrets in Development) rather than hardcoded in source.
    /// Bound as a dictionary keyed by role name, e.g. "SeedAccounts:Admin:Email".
    /// </summary>
    public sealed class SeedAccountOptions
    {
        public const string SectionName = "SeedAccounts";

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
