namespace StewardshipSurvey.Models
{
    /// <summary>
    /// Whether a person is an established member of the congregation or is still
    /// considering it. Chosen by the person on their own profile, and changeable by an
    /// administrator. Null means they have not answered yet.
    /// </summary>
    public enum MembershipStatus
    {
        Member = 0,
        ProspectiveMember = 1
    }
}
