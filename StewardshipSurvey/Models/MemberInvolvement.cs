using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StewardshipSurvey.Models
{
    public class MemberInvolvement
    {
        [Key]
        public int MemberInvolvementID { get; set; }

        [ForeignKey("Member")]
        public int MemberID { get; set; }
        public MemberInfo? Member { get; set; }

        [ForeignKey("InvolvementArea")]
        public int InvolvementAreaID { get; set; }
        public InvolvementAreas? InvolvementArea { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        
    }
}
