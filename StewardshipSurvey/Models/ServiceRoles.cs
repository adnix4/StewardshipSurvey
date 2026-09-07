//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;

//namespace StewardshipSurvey.Models
//{
//    public class ServiceRoles
//    {
//        [Key]
//        public int ServiceRoleID { get; set; }
//        [Required(ErrorMessage = "Service Role is required")]
//        [Display(Name = "Service Role")]
//        public required string ServiceRole { get; set; }
//        [Display(Name = "Description")]
//        public string? Description { get; set; }
//        public bool IsActive { get; set; } = true;
//        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
//        public DateTime? UpdatedDate { get; set; }
//        public ICollection<MemberServiceRole>? MemberServiceRoles { get; set; }
//    }
//}
