using Microsoft.AspNetCore.Identity;
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
            }

            if (confirmedByBackfill > 0)
            {
                logger.LogInformation(
                    "Confirmed {Count} account(s) that predate email confirmation being enforced.",
                    confirmedByBackfill);
            }

        }
    }
}
