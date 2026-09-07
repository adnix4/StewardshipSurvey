namespace StewardshipSurvey.Data
{
    /// <summary>
    /// The application's Identity role names.
    /// <para>
    /// <see cref="RegisteredUser"/> is a catch-all held by every account that can sign in.
    /// The other three stack on top of it rather than replacing it, so an administrator is
    /// both <see cref="Admin"/> and <see cref="RegisteredUser"/>.
    /// </para>
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string VolunteerOrganizer = "VolunteerOrganizer";
        public const string RegisteredUser = "RegisteredUser";

        /// <summary>Mirrors <c>MemberInfo.MembershipStatus</c>; never assigned directly.</summary>
        public const string Member = "Member";

        /// <summary>Mirrors <c>MemberInfo.MembershipStatus</c>; never assigned directly.</summary>
        public const string ProspectiveMember = "ProspectiveMember";

        public static readonly string[] All =
            { Admin, Staff, VolunteerOrganizer, RegisteredUser, Member, ProspectiveMember };

        /// <summary>
        /// Roles derived from membership status rather than granted by an administrator.
        /// The Edit Roles screen shows these read-only.
        /// </summary>
        public static readonly string[] StatusMirrored = { Member, ProspectiveMember };
    }
}
