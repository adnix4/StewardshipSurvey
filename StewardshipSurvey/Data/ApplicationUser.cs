using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Data
{
    public class ApplicationUser : IdentityUser
    {
       public int? MemberID { get; set; }
         [ForeignKey("MemberID")]
         public MemberInfo? Member { get; set; }
       
       
        /// <summary>
        /// When this account was deactivated by an administrator, or null if it is active.
        /// Non-null is the definition of "deactivated" and is what the retention purge reads.
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        /// <summary>
        /// Comma-delimited snapshot of the elevated roles stripped at deactivation, so an
        /// administrator can choose to restore them when reactivating. Null or empty means
        /// the account held none.
        /// </summary>
        public string? DeactivatedRoles { get; set; }

        // Additional properties can be added as needed
    }
}
