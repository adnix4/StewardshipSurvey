using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StewardshipSurvey.Data;

namespace StewardshipSurvey.Services
{
    /// <summary>
    /// Permanently deletes accounts that have been deactivated longer than the retention
    /// window, so deactivated rows do not accumulate indefinitely.
    /// <para>
    /// A sweep runs shortly after startup and then on the configured interval. The startup
    /// sweep matters: a bare monthly timer would never fire on a server that restarts more
    /// often than monthly.
    /// </para>
    /// </summary>
    public class DeactivatedUserPurgeService : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UserRetentionOptions _options;
        private readonly ILogger<DeactivatedUserPurgeService> _logger;

        public DeactivatedUserPurgeService(
            IServiceScopeFactory scopeFactory,
            IOptions<UserRetentionOptions> options,
            ILogger<DeactivatedUserPurgeService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.PurgeDeactivatedAfterDays <= 0)
            {
                _logger.LogInformation(
                    "{Section}:PurgeDeactivatedAfterDays is {Days}; deactivated accounts will never be purged.",
                    UserRetentionOptions.SectionName,
                    _options.PurgeDeactivatedAfterDays);
                return;
            }

            var interval = TimeSpan.FromDays(Math.Max(1, _options.CheckIntervalDays));

            try
            {
                await Task.Delay(StartupDelay, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await PurgeAsync(stoppingToken);
                    await Task.Delay(interval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        internal async Task PurgeAsync(CancellationToken cancellationToken)
        {
            // This service is a singleton; UserManager and the DbContext are scoped.
            using var scope = _scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow.AddDays(-_options.PurgeDeactivatedAfterDays);

            var candidates = await userManager.Users
                .Where(u => u.DeactivatedDate != null && u.DeactivatedDate < cutoff)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("Purge sweep: no accounts deactivated before {Cutoff:u}.", cutoff);
                return;
            }

            var purged = 0;

            foreach (var user in candidates)
            {
                // Belt and braces. Deactivation strips the elevated roles, so a deactivated
                // account should never still hold Admin - but never purge the last one.
                if (await userManager.IsInRoleAsync(user, Roles.Admin))
                {
                    var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
                    if (admins.Count <= 1)
                    {
                        _logger.LogWarning(
                            "Skipping purge of {Email}: it is the only remaining {Role} account.",
                            user.Email, Roles.Admin);
                        continue;
                    }
                }

                var email = user.Email;
                var deactivatedOn = user.DeactivatedDate;
                var memberId = user.MemberID;

                // Order matters. AspNetUsers.MemberID is the foreign key with NO ACTION, so
                // the user row must go first; deleting the profile then cascades the member's
                // interest, involvement and service-role rows.
                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError(
                        "Could not purge {Email}: {Errors}",
                        email, string.Join("; ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                if (memberId != null)
                {
                    var profile = await context.MemberInfos
                        .FirstOrDefaultAsync(m => m.MemberID == memberId, cancellationToken);

                    if (profile != null)
                    {
                        context.MemberInfos.Remove(profile);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }

                purged++;
                _logger.LogWarning(
                    "Purged {Email}: deactivated {DeactivatedOn:u}, past the {Days}-day retention window.",
                    email, deactivatedOn, _options.PurgeDeactivatedAfterDays);
            }

            _logger.LogInformation(
                "Purge sweep complete: {Purged} of {Candidates} eligible account(s) removed.",
                purged, candidates.Count);
        }
    }
}
