using Microsoft.AspNetCore.Identity;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Data
{
    /// <summary>
    /// Keeps the Member / ProspectiveMember Identity roles in step with
    /// <see cref="MemberInfo.MembershipStatus"/>, which is the source of truth.
    /// <para>
    /// The roles exist so views and navigation can test membership status with a plain
    /// <c>User.IsInRole</c> check instead of a database round-trip. Nothing should assign
    /// them directly - call <see cref="SyncAsync"/> whenever the status changes.
    /// </para>
    /// </summary>
    public static class MembershipStatusRoles
    {
        /// <summary>The role mirroring a status, or null when the status is unanswered.</summary>
        public static string? RoleFor(MembershipStatus? status) => status switch
        {
            MembershipStatus.Member => Roles.Member,
            MembershipStatus.ProspectiveMember => Roles.ProspectiveMember,
            _ => null
        };

        /// <summary>
        /// Gives <paramref name="user"/> exactly the role matching <paramref name="status"/>,
        /// removing the other. An unanswered status leaves the user in neither.
        /// </summary>
        public static async Task SyncAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            MembershipStatus? status)
        {
            var desired = RoleFor(status);

            foreach (var role in Roles.StatusMirrored)
            {
                var inRole = await userManager.IsInRoleAsync(user, role);

                if (role == desired && !inRole)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                else if (role != desired && inRole)
                {
                    await userManager.RemoveFromRoleAsync(user, role);
                }
            }
        }
    }
}
