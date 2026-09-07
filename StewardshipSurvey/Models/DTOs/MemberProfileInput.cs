using System.ComponentModel.DataAnnotations;

namespace StewardshipSurvey.Models.DTOs
{
    /// <summary>
    /// The fields a member may set on their own profile form, and nothing else.
    /// <para>
    /// The page used to bind <see cref="MemberInfo"/> - the EF entity - directly. A crafted
    /// POST could therefore carry <c>MemberID</c>, <c>IsActive</c>, <c>CreatedDate</c>,
    /// <c>ApplicationUserID</c> and the three navigation collections alongside the form's real
    /// fields. The update path happened to copy a fixed list of properties, which blunted it,
    /// but the insert path added the bound object to the context exactly as it arrived.
    /// </para>
    /// <para>
    /// With a dedicated input type the extra values have nowhere to land: the binder discards
    /// anything not declared here. Adding a field to the form now means adding it here first.
    /// That is the mechanism working, not an obstacle to it.
    /// </para>
    /// </summary>
    public class MemberProfileInput
    {
        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First Name is required")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last Name is required")]
        public string? LastName { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Zip { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Cell Phone Number")]
        public string? CellPhoneNumber { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? HomePhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? Email { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid preferred email address")]
        [Display(Name = "Preferred Contact Email")]
        public string? PreferredContactEmail { get; set; }

        // Reuses the entity's validator rather than restating the rule, so the two cannot
        // drift apart.
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        [CustomValidation(typeof(MemberInfo), nameof(MemberInfo.ValidateBirthDate))]
        public DateTime? BirthDate { get; set; }

        public string? Sex { get; set; }

        public string? Comments { get; set; }

        [Display(Name = "Membership Status")]
        public MembershipStatus? MembershipStatus { get; set; }

        // Set from the PreferredContact radio group, not posted directly.
        [Display(Name = "Preffered Contact Method")]
        public bool PrefersPhone { get; set; }
        public bool PrefersEmail { get; set; }
        public bool PrefersText { get; set; }

        /// <summary>Fills the form from a stored profile.</summary>
        public static MemberProfileInput FromEntity(MemberInfo member) => new()
        {
            FirstName = member.FirstName,
            LastName = member.LastName,
            Address = member.Address,
            City = member.City,
            State = member.State,
            Zip = member.Zip,
            CellPhoneNumber = member.CellPhoneNumber,
            HomePhoneNumber = member.HomePhoneNumber,
            Email = member.Email,
            PreferredContactEmail = member.PreferredContactEmail,
            BirthDate = member.BirthDate,
            Sex = member.Sex,
            Comments = member.Comments,
            MembershipStatus = member.MembershipStatus,
            PrefersPhone = member.PrefersPhone,
            PrefersEmail = member.PrefersEmail,
            PrefersText = member.PrefersText
        };

        /// <summary>
        /// Copies the editable fields onto a profile. The single place that decides what a
        /// member may change about themselves - both the insert and the update path go
        /// through it, so the two cannot disagree.
        /// </summary>
        public void ApplyTo(MemberInfo member)
        {
            member.FirstName = FirstName;
            member.LastName = LastName;
            member.Address = Address;
            member.City = City;
            member.State = State;
            member.Zip = Zip;
            member.CellPhoneNumber = CellPhoneNumber;
            member.HomePhoneNumber = HomePhoneNumber;
            member.Email = Email;
            member.PreferredContactEmail = PreferredContactEmail;
            member.BirthDate = BirthDate;
            member.Sex = Sex;
            member.Comments = Comments;
            member.MembershipStatus = MembershipStatus;
            member.PrefersPhone = PrefersPhone;
            member.PrefersEmail = PrefersEmail;
            member.PrefersText = PrefersText;
        }
    }
}
