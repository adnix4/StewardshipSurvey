using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StewardshipSurvey.Models
{
    public class InvolvementAreas
    {
        [Key]
        public int InvolvementAreaID { get; set; }

        [Required(ErrorMessage = "Area of Involvement is required")]
        [Display(Name = "Area of Involvement")]
        public string AreaOfInvolvement { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public ICollection<MemberInvolvement>? MemberInvolvements { get; set; }

        public ICollection<MemberServiceRole>? MemberServiceRoles { get; set; }
    }
}
