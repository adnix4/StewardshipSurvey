namespace StewardshipSurvey.Models
{
    public class MemberServiceRole
    {
        public int MemberID { get; set; }
        public MemberInfo? Member { get; set; }

        public int InvolvementAreaID { get; set; }
        public InvolvementAreas? InvolvementArea { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}

