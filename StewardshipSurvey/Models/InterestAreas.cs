using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StewardshipSurvey.Models
{
    public class InterestAreas
    {
        [Key]
        public int InterestAreaID { get; set; }
        
        [Required(ErrorMessage = "Interest Area is required")]
        [Display(Name = "Interest Area")]
        public string InterestArea { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public ICollection<MemberInterest>? MemberInterests { get; set; }
    }
}
