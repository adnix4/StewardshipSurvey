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

        /// <summary>
        /// How long after startup the first sweep waits, giving the application time to finish
        /// coming up before a background job starts deleting rows.
        /// <para>
        /// Was a hardcoded <c>TimeSpan.FromMinutes(1)</c> in the service. Configuration rather
        /// than a clock abstraction is deliberate: the purge tests call <c>PurgeAsync</c>
        /// directly and set <c>DeactivatedDate</c> far in the past, so they never wait and have
        /// no need of an injectable clock. A <c>TimeProvider</c> seam would buy only an
        /// exact-boundary test, at the cost of a seam nothing else asks for.
        /// </para>
        /// </summary>
        public int StartupDelayMinutes { get; set; } = 1;
    }
}
