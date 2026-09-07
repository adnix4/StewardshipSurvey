namespace StewardshipSurvey.Data
{
    /// <summary>
    /// How long a deactivated account is kept before it is permanently purged.
    /// Bound from the "UserRetention" configuration section.
    /// </summary>
    public sealed class UserRetentionOptions
    {
        public const string SectionName = "UserRetention";

        /// <summary>
        /// Days an account may stay deactivated before it is deleted outright.
        /// Defaults to three years. Set to 0 to disable purging entirely.
        /// </summary>
        public int PurgeDeactivatedAfterDays { get; set; } = 1095;

        /// <summary>
        /// How often the purge sweep runs. A sweep also runs shortly after startup, so a
        /// server that restarts more often than this interval still gets swept.
        /// </summary>
        public int CheckIntervalDays { get; set; } = 30;
    }
}
