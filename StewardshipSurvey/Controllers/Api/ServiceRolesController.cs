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
    // Default deny. Without this the class opts in per action, so an action added later with
    // no attribute is public - the failure mode is silence, which is the wrong way round for
    // authorisation. The catalogue endpoints below opt back out explicitly.
    [Authorize]
    public class ServiceRolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceRolesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<List<InvolvementAreaDto>>> GetAllServiceRoles()
        {
            var roles = await _context.InvolvementAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.AreaOfInvolvement)
                .Select(ia => new InvolvementAreaDto
                {
                    InvolvementAreaID = ia.InvolvementAreaID,
                    AreaOfInvolvement = ia.AreaOfInvolvement,
                    Description = ia.Description ?? string.Empty,
                    IsActive = ia.IsActive
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<List<MemberServiceRoleDto>>> GetCurrentMemberServiceRoles()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            var roles = await _context.MemberServiceRoles
                .Where(msr => msr.MemberID == user.MemberID)
                .Include(msr => msr.InvolvementArea)
                .Select(msr => new MemberServiceRoleDto
                {
                    MemberID = msr.MemberID,
                    InvolvementAreaID = msr.InvolvementAreaID,
                    AreaOfInvolvement = msr.InvolvementArea!.AreaOfInvolvement
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost("current")]
        [Authorize]
        public async Task<ActionResult> UpdateCurrentMemberServiceRoles([FromBody] List<int> involvementAreaIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            int memberId = user.MemberID.Value;

            var existing = _context.MemberServiceRoles.Where(msr => msr.MemberID == memberId);
            _context.MemberServiceRoles.RemoveRange(existing);

            foreach (var involvementId in involvementAreaIds)
            {
                _context.MemberServiceRoles.Add(new MemberServiceRole
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
