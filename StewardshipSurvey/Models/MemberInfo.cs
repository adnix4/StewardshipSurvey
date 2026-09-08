using System;
using System.ComponentModel.DataAnnotations;
using StewardshipSurvey.Data;

namespace StewardshipSurvey.Models
{
    public class MemberInfo
    {
        [Key]
        public int MemberID { get; set; }
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

        public string? WorkPhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? Email { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid preferred email address")]
        [Display(Name = "Preferred Contact Email")]
        public string? PreferredContactEmail { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        [CustomValidation(typeof(MemberInfo), nameof(ValidateBirthDate))]
        public DateTime? BirthDate { get; set; }

        public string? Sex { get; set; }

        public string? Comments { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        // Null until the person answers. Mirrored onto the Member / ProspectiveMember
        // Identity roles when the profile is saved, so views and nav can check it cheaply.
        [Display(Name = "Membership Status")]
        public MembershipStatus? MembershipStatus { get; set; }

        [Display(Name = "Preffered Contact Method")]
        public bool PrefersPhone { get; set; }
        public bool PrefersEmail { get; set; }
        public bool PrefersText { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }
        public ICollection<MemberInterest> MemberInterests { get; set; } = new List<MemberInterest>();
        public ICollection<MemberInvolvement> MemberInvolvements { get; set; } = new List<MemberInvolvement>();
        public ICollection<MemberServiceRole> MemberServiceRoles { get; set; } = new List<MemberServiceRole>();

        // Custom validation method for BirthDate
        public static ValidationResult? ValidateBirthDate(DateTime? birthDate, ValidationContext context)
        {
            if (birthDate.HasValue)
            {
                if (birthDate.Value > DateTime.UtcNow)
                {
                    return new ValidationResult("Birth Date cannot be in the future.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
