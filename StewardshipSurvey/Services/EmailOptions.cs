namespace StewardshipSurvey.Services
{
    /// <summary>
    /// Outgoing mail configuration, bound from the "Email" section. Credentials belong in
    /// user secrets, never in appsettings - see README.md.
    /// <para>
    /// When <see cref="SmtpOptions.Host"/> is absent the application falls back to
    /// <see cref="FileDropEmailSender"/>, so the confirmation flow still works end to end on
    /// a machine with no mail server.
    /// </para>
    /// </summary>
    public sealed class EmailOptions
    {
        public const string SectionName = "Email";

        /// <summary>Address confirmation mail is sent from.</summary>
        public string From { get; set; } = "noreply@stmark.local";

        /// <summary>Where <see cref="FileDropEmailSender"/> writes .eml files.</summary>
        public string FileDropPath { get; set; } = "App_Data/mail";

        public SmtpOptions Smtp { get; set; } = new();
    }

    public sealed class SmtpOptions
    {
        /// <summary>Null or empty selects the file-drop sender instead of real SMTP.</summary>
        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        public string? User { get; set; }

        public string? Password { get; set; }
    }
}
