using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace StewardshipSurvey.Services
{
    /// <summary>
    /// Sends mail through a real SMTP server. Selected only when <c>Email:Smtp:Host</c> is
    /// configured.
    /// <para>
    /// Uses <see cref="SmtpClient"/> from the base class library so no package is needed.
    /// Microsoft recommends MailKit for new work; because this sits behind
    /// <see cref="IEmailSender"/>, replacing it is a one-class change.
    /// </para>
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtp = _options.Smtp;

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(smtp.User))
            {
                client.Credentials = new NetworkCredential(smtp.User, smtp.Password);
            }

            using var message = new MailMessage(_options.From, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Sent \"{Subject}\" to {Email} via {Host}.", subject, email, smtp.Host);
            }
            catch (Exception ex)
            {
                // Never swallow this. A silent no-op sender is the bug this class exists to
                // fix; a silently failing one would be the same bug wearing a disguise.
                _logger.LogError(ex,
                    "Failed to send \"{Subject}\" to {Email} via {Host}:{Port}. " +
                    "The account cannot be confirmed until mail delivery works.",
                    subject, email, smtp.Host, smtp.Port);
                throw;
            }
        }
    }
}
