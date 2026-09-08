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
    public class InterestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<List<InterestAreaDto>>> GetAllInterests()
        {
            var interests = await _context.InterestAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.InterestArea)
                .Select(ia => new InterestAreaDto
                {
                    InterestAreaID = ia.InterestAreaID,
                    InterestArea = ia.InterestArea,
                    Description = ia.Description ?? string.Empty,
                    IsActive = ia.IsActive
                })
                .ToListAsync();

            return Ok(interests);
        }

        [HttpGet("current")]
        public async Task<ActionResult<List<MemberInterestDto>>> GetCurrentMemberInterests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            var interests = await _context.MemberInterests
                .Where(mi => mi.MemberID == user.MemberID)
                .Include(mi => mi.InterestArea)
                .Select(mi => new MemberInterestDto
                {
                    MemberID = mi.MemberID,
                    InterestAreaID = mi.InterestAreaID,
                    InterestArea = mi.InterestArea!.InterestArea
                })
                .ToListAsync();

            return Ok(interests);
        }

        [HttpPost("current")]
        public async Task<ActionResult> UpdateCurrentMemberInterests([FromBody] List<int> interestAreaIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            int memberId = user.MemberID.Value;

            // Distinct, because the key is (MemberID, InterestAreaID): a body repeating an id
            // used to violate the primary key and come back a 500. The Razor pages have always
            // done this; the API never did.
            var ids = (interestAreaIds ?? new List<int>()).Distinct().ToList();

            // An id that does not exist used to reach the database and fail the foreign key,
            // which also surfaced as a 500 - the caller's mistake reported as the server's.
            var known = await _context.InterestAreas
                .Where(a => ids.Contains(a.InterestAreaID))
                .Select(a => a.InterestAreaID)
                .ToListAsync();

            var unknown = ids.Except(known).ToList();
            if (unknown.Count > 0)
            {
                return BadRequest(new
                {
                    message = $"Unknown interest area id(s): {string.Join(", ", unknown)}"
                });
            }

            var existing = _context.MemberInterests.Where(mi => mi.MemberID == memberId);
            _context.MemberInterests.RemoveRange(existing);

            foreach (var interestId in ids)
            {
                _context.MemberInterests.Add(new MemberInterest
                {
                    MemberID = memberId,
                    InterestAreaID = interestId,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
