namespace TestUserLogIn.Maui.Models;

public class MemberDto
{
    public int MemberID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string CellPhoneNumber { get; set; }
    public string HomePhoneNumber { get; set; }
    public string WorkPhoneNumber { get; set; }
    public string PreferredContactEmail { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Sex { get; set; }
    public string Comments { get; set; }
    public bool IsActive { get; set; }
    public bool PrefersPhone { get; set; }
    public bool PrefersEmail { get; set; }
    public bool PrefersText { get; set; }
}

public class InterestAreaDto
{
    public int InterestAreaID { get; set; }
    public string InterestArea { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}

public class InvolvementAreaDto
{
    public int InvolvementAreaID { get; set; }
    public string AreaOfInvolvement { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
}

public class MemberServiceRoleDto
{
    public int MemberID { get; set; }
    public int InvolvementAreaID { get; set; }
    public string AreaOfInvolvement { get; set; }
}

public class MemberInterestDto
{
    public int MemberID { get; set; }
    public int InterestAreaID { get; set; }
    public string InterestArea { get; set; }
}

public class MemberInvolvementDto
{
    public int MemberID { get; set; }
    public int InvolvementAreaID { get; set; }
    public string AreaOfInvolvement { get; set; }
}
