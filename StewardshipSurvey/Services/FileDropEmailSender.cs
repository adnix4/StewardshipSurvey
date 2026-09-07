using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace StewardshipSurvey.Services
{
    /// <summary>
    /// Writes outgoing mail to disk as .eml files instead of sending it. Selected when no
    /// SMTP host is configured, so the confirmation flow can be walked end to end on a
    /// machine with no mail server - including CI.
    /// <para>
    /// This is deliberately not a no-op. The framework's NoOpEmailSender discarded messages
    /// silently, which is what let a broken confirmation flow look healthy for so long.
    /// </para>
    /// </summary>
    public class FileDropEmailSender : IEmailSender
    {
        private static readonly Regex LinkPattern =
            new Regex(@"href='(?<url>[^']+)'", RegexOptions.Compiled);

        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileDropEmailSender> _logger;

        public FileDropEmailSender(
            IOptions<EmailOptions> options,
            IWebHostEnvironment environment,
            ILogger<FileDropEmailSender> logger)
        {
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var directory = Path.IsPathRooted(_options.FileDropPath)
                ? _options.FileDropPath
                : Path.Combine(_environment.ContentRootPath, _options.FileDropPath);

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory,
                $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss-fff}_{Sanitize(email)}.eml");

            var eml = new StringBuilder()
                .AppendLine($"From: {_options.From}")
                .AppendLine($"To: {email}")
                .AppendLine($"Subject: {subject}")
                .AppendLine($"Date: {DateTime.UtcNow:r}")
                .AppendLine("MIME-Version: 1.0")
                .AppendLine("Content-Type: text/html; charset=utf-8")
                .AppendLine()
                .AppendLine(htmlMessage)
                .ToString();

            await File.WriteAllTextAsync(path, eml, Encoding.UTF8);

            // Log the link so it can be followed straight from the console during a demo.
            var link = LinkPattern.Match(htmlMessage) is { Success: true } match
                ? match.Groups["url"].Value
                : "(no link found in message)";

            _logger.LogInformation(
                "No SMTP host configured, so \"{Subject}\" for {Email} was written to {Path} " +
                "instead of being sent. Link: {Link}",
                subject, email, path, link);
        }

        /// <summary>Makes an address safe to use as a file name.</summary>
        private static string Sanitize(string email)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(email.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }
    }
}
