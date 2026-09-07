namespace StewardshipSurvey.Models.DTOs
{
    public class MemberReportDto
    {
        public int MemberID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CellPhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> ServiceRoles { get; set; } = new();
        public List<string> Interests { get; set; } = new();
        public List<string> InvolvementAreas { get; set; } = new();
    }

    public class MemberDto
    {
        public int MemberID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CellPhoneNumber { get; set; } = string.Empty;
        public string HomePhoneNumber { get; set; } = string.Empty;
        public string WorkPhoneNumber { get; set; } = string.Empty;
        public string PreferredContactEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool PrefersPhone { get; set; }
        public bool PrefersEmail { get; set; }
        public bool PrefersText { get; set; }
    }

    public class MemberServiceRoleDto
    {
        public int MemberID { get; set; }
        public int InvolvementAreaID { get; set; }
        public string AreaOfInvolvement { get; set; } = string.Empty;
    }

    public class MemberInterestDto
    {
        public int MemberID { get; set; }
        public int InterestAreaID { get; set; }
        public string InterestArea { get; set; } = string.Empty;
    }

    public class MemberInvolvementDto
    {
        public int MemberID { get; set; }
        public int InvolvementAreaID { get; set; }
        public string AreaOfInvolvement { get; set; } = string.Empty;
    }

    public class InvolvementAreaDto
    {
        public int InvolvementAreaID { get; set; }
        public string AreaOfInvolvement { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class InterestAreaDto
    {
        public int InterestAreaID { get; set; }
        public string InterestArea { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
