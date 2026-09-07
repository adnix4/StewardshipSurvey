using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using System.ComponentModel.DataAnnotations.Schema;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Data
{
    public class ApplicationUser : IdentityUser
    {
       public int? MemberID { get; set; }
         [ForeignKey("MemberID")]
         public MemberInfo? Member { get; set; }
       
       
        // Additional properties can be added as needed
    }
}
