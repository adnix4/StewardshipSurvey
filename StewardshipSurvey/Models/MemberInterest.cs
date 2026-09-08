using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StewardshipSurvey.Models
{
    public class MemberInterest
    {
        [ForeignKey("Member")]
        public int MemberID { get; set; }
        public MemberInfo? Member { get; set; }

        [ForeignKey("InterestArea")]
        public int InterestAreaID { get; set; }
        public InterestAreas? InterestArea { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

    }
}
