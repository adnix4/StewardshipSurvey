using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Models.DTOs;

namespace StewardshipSurvey.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvolvementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InvolvementsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<List<InvolvementAreaDto>>> GetAllInvolvements()
        {
            var involvements = await _context.InvolvementAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.AreaOfInvolvement)
                .Select(ia => new InvolvementAreaDto
                {
                    InvolvementAreaID = ia.InvolvementAreaID,
                    AreaOfInvolvement = ia.AreaOfInvolvement,
                    Description = ia.Description,
                    IsActive = ia.IsActive
                })
                .ToListAsync();

            return Ok(involvements);
        }

        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<List<MemberInvolvementDto>>> GetCurrentMemberInvolvements()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            var involvements = await _context.MemberInvolvements
                .Where(mi => mi.MemberID == user.MemberID)
                .Include(mi => mi.InvolvementArea)
                .Select(mi => new MemberInvolvementDto
                {
                    MemberID = mi.MemberID,
                    InvolvementAreaID = mi.InvolvementAreaID,
                    AreaOfInvolvement = mi.InvolvementArea.AreaOfInvolvement
                })
                .ToListAsync();

            return Ok(involvements);
        }

        [HttpPost("current")]
        [Authorize]
        public async Task<ActionResult> UpdateCurrentMemberInvolvements([FromBody] List<int> involvementAreaIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            int memberId = user.MemberID.Value;

            var existing = _context.MemberInvolvements.Where(mi => mi.MemberID == memberId);
            _context.MemberInvolvements.RemoveRange(existing);

            foreach (var involvementId in involvementAreaIds)
            {
                _context.MemberInvolvements.Add(new MemberInvolvement
                {
                    MemberID = memberId,
                    InvolvementAreaID = involvementId,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
