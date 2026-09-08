using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Models;
using System.Linq;

namespace StewardshipSurvey.Data
{
    public class AdminSeeder
    {
        /// <summary>
        /// Creates any missing Identity roles. Runs in every environment: roles are part of
        /// the application's structure, not development seed data, and
        /// <c>UserManager.AddToRoleAsync</c> throws when a role is absent - so without this,
        /// registration fails outright anywhere the development seeder does not run.
        /// </summary>
        public static async Task EnsureRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger<AdminSeeder>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles = Roles.All;

            // Seed account credentials come from configuration (user secrets in Development),
            // never from source. Keyed by role name, e.g. "SeedAccounts:Admin:Email".
            var seedAccounts = configuration
                .GetSection(SeedAccountOptions.SectionName)
                .Get<Dictionary<string, SeedAccountOptions>>();

            if (seedAccounts == null || seedAccounts.Count == 0)
            {
                logger.LogWarning(
                    "No {Section} configured - skipping seed account creation. To create them, run " +
                    "'dotnet user-secrets set \"{Section}:Admin:Email\" \"<email>\"' (and :Password) " +
                    "from the StewardshipSurvey project folder. See README.md.",
                    SeedAccountOptions.SectionName,
                    SeedAccountOptions.SectionName);
            }
            else
            {
                foreach (var (role, account) in seedAccounts)
                {
                    if (!roles.Contains(role))
                    {
                        logger.LogWarning(
                            "Seed account is configured for unknown role '{Role}' - skipping.", role);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(account.Email) || string.IsNullOrWhiteSpace(account.Password))
                    {
                        logger.LogWarning(
                            "Seed account for role '{Role}' is missing an email or password - skipping.", role);
                        continue;
                    }

                    // Create the user if it doesn't exist
                    var user = await userManager.FindByEmailAsync(account.Email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = account.Email,
                            Email = account.Email,
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(user, account.Password);
                        if (!result.Succeeded)
                        {
                            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                            throw new Exception($"Seed account creation failed for role '{role}': {errors}");
                        }

                        logger.LogInformation("Created seed account for role '{Role}'.", role);
                    }

                    // Assign the user to its role
                    if (!await userManager.IsInRoleAsync(user, role))
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            // RegisteredUser is the catch-all every account holds, on top of any elevated
            // role. Backfill it for anyone missing it, including the seed accounts above.
            var users = userManager.Users.ToList();

            var confirmedByBackfill = 0;
            var statusRolesRepaired = 0;

            foreach (var user in users)
            {
                var assignedRoles = await userManager.GetRolesAsync(user);

                if (!assignedRoles.Contains(Roles.RegisteredUser))
                {
                    await userManager.AddToRoleAsync(user, Roles.RegisteredUser);
                }

                // Accounts created before email confirmation was enforceable would otherwise
                // be locked out by a bug that was never theirs. One-time, Development only.
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                    confirmedByBackfill++;
                }

                // MembershipStatusRoles.SyncAsync only ever runs from a write - the member's
                // own profile save, or an admin edit - so an account that has not been saved
                // since the mirrored-role design arrived keeps whatever roles it had. Two
                // accounts held Member with no MembershipStatus at all, a combination the sync
                // cannot produce and nothing repaired. Reconciling here means it self-heals
                // instead of drifting further.
                var profile = await context.MemberInfos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ApplicationUser!.Id == user.Id);

                if (await StatusRolesAreStaleAsync(userManager, user, profile?.MembershipStatus))
                {
                    await MembershipStatusRoles.SyncAsync(userManager, user, profile?.MembershipStatus);
                    statusRolesRepaired++;
                }
            }

            if (statusRolesRepaired > 0)
            {
                logger.LogInformation(
                    "Reconciled the membership status role on {Count} account(s).",
                    statusRolesRepaired);
            }

            if (confirmedByBackfill > 0)
            {
                logger.LogInformation(
                    "Confirmed {Count} account(s) that predate email confirmation being enforced.",
                    confirmedByBackfill);
            }

        }

        /// <summary>
        /// True when the account's status-mirrored roles do not match <paramref name="status"/>.
        /// Checked before syncing so the log line counts real repairs rather than every account.
        /// </summary>
        internal static async Task<bool> StatusRolesAreStaleAsync(
            UserManager<ApplicationUser> userManager, ApplicationUser user, MembershipStatus? status)
        {
            var desired = MembershipStatusRoles.RoleFor(status);

            foreach (var role in Roles.StatusMirrored)
            {
                if (await userManager.IsInRoleAsync(user, role) != (role == desired))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
