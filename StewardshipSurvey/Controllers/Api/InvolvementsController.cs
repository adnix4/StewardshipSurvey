using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    // Default deny, and bearer only. Without the class-level attribute an action added later
    // with no attribute is public, and without the scheme a browser cookie is accepted -
    // which, with no antiforgery check on an [ApiController], is a cross-site write.
    // The catalogue endpoint below opts back out explicitly.
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                    Description = ia.Description ?? string.Empty,
                    IsActive = ia.IsActive
                })
                .ToListAsync();

            return Ok(involvements);
        }

        [HttpGet("current")]
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
                    AreaOfInvolvement = mi.InvolvementArea!.AreaOfInvolvement
                })
                .ToListAsync();

            return Ok(involvements);
        }

        [HttpPost("current")]
        public async Task<ActionResult> UpdateCurrentMemberInvolvements([FromBody] List<int> involvementAreaIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            int memberId = user.MemberID.Value;

            // Distinct, because the key is (MemberID, InvolvementAreaID): a body repeating an id
            // used to violate the primary key and come back a 500. The Razor pages have always
            // done this; the API never did.
            var ids = (involvementAreaIds ?? new List<int>()).Distinct().ToList();

            // An id that does not exist used to reach the database and fail the foreign key,
            // which also surfaced as a 500 - the caller's mistake reported as the server's.
            var known = await _context.InvolvementAreas
                .Where(a => ids.Contains(a.InvolvementAreaID))
                .Select(a => a.InvolvementAreaID)
                .ToListAsync();

            var unknown = ids.Except(known).ToList();
            if (unknown.Count > 0)
            {
                return BadRequest(new
                {
                    message = $"Unknown involvement area id(s): {string.Join(", ", unknown)}"
                });
            }

            var existing = _context.MemberInvolvements.Where(mi => mi.MemberID == memberId);
            _context.MemberInvolvements.RemoveRange(existing);

            foreach (var involvementId in ids)
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
