using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestUserLogIn.Data;
using TestUserLogIn.Models;
using TestUserLogIn.Models.DTOs;

namespace TestUserLogIn.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
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
                    Description = ia.Description,
                    IsActive = ia.IsActive
                })
                .ToListAsync();

            return Ok(interests);
        }

        [HttpGet("current")]
        [Authorize]
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
                    InterestArea = mi.InterestArea.InterestArea
                })
                .ToListAsync();

            return Ok(interests);
        }

        [HttpPost("current")]
        [Authorize]
        public async Task<ActionResult> UpdateCurrentMemberInterests([FromBody] List<int> interestAreaIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            int memberId = user.MemberID.Value;

            var existing = _context.MemberInterests.Where(mi => mi.MemberID == memberId);
            _context.MemberInterests.RemoveRange(existing);

            foreach (var interestId in interestAreaIds)
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
