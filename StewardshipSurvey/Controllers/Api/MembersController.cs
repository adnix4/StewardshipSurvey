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
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("current")]
        public async Task<ActionResult<MemberDto>> GetCurrentMember()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return NotFound("Member not found for current user");

            var member = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.MemberID == user.MemberID);

            if (member == null)
                return NotFound();

            return Ok(MapToMemberDto(member));
        }

        /// <summary>
        /// Reads one member profile by id. A caller may read their own profile; Staff and
        /// Admin may read any.
        /// <para>
        /// The ownership check is the point of this method. Without it the class-level
        /// <c>[Authorize]</c> was the only gate, so any signed-in account could walk
        /// <c>id = 1..n</c> and pull every member's home address, birth date, sex and phone
        /// numbers. Registration is public, so "any signed-in account" meant anyone.
        /// </para>
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<MemberDto>> GetMember(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var isOwnProfile = user.MemberID != null && user.MemberID == id;
            var isPrivileged = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);

            // Decided before the lookup on purpose. Answering 403 for a forbidden id but 404
            // for a missing one would still let a caller map which ids exist.
            //
            // Not Forbid(): with no bearer scheme registered this endpoint authenticates by
            // cookie, and the cookie handler turns a forbid into a 302 to the Access Denied
            // page - an HTML redirect the mobile client cannot interpret. Verified in
            // MembersApiTests. Say 403 outright until the bearer scheme lands.
            if (!isOwnProfile && !isPrivileged)
                return StatusCode(StatusCodes.Status403Forbidden);

            var member = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.MemberID == id);

            if (member == null)
                return NotFound();

            return Ok(MapToMemberDto(member));
        }

        [HttpPut("current")]
        public async Task<ActionResult> UpdateCurrentMember([FromBody] MemberDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MemberID == null)
                return Unauthorized();

            var member = await _context.MemberInfos
                .FirstOrDefaultAsync(m => m.MemberID == user.MemberID);

            if (member == null)
                return NotFound();

            member.FirstName = dto.FirstName;
            member.LastName = dto.LastName;
            member.Email = dto.Email;
            member.CellPhoneNumber = dto.CellPhoneNumber;
            member.HomePhoneNumber = dto.HomePhoneNumber;
            member.WorkPhoneNumber = dto.WorkPhoneNumber;
            member.PreferredContactEmail = dto.PreferredContactEmail;
            member.Address = dto.Address;
            member.City = dto.City;
            member.State = dto.State;
            member.Zip = dto.Zip;
            member.BirthDate = dto.BirthDate;
            member.Sex = dto.Sex;
            member.Comments = dto.Comments;
            member.PrefersPhone = dto.PrefersPhone;
            member.PrefersEmail = dto.PrefersEmail;
            member.PrefersText = dto.PrefersText;
            member.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }

        private MemberDto MapToMemberDto(MemberInfo member)
        {
            return new MemberDto
            {
                MemberID = member.MemberID,
                FirstName = member.FirstName ?? string.Empty,
                LastName = member.LastName ?? string.Empty,
                Email = member.Email ?? string.Empty,
                CellPhoneNumber = member.CellPhoneNumber ?? string.Empty,
                HomePhoneNumber = member.HomePhoneNumber ?? string.Empty,
                WorkPhoneNumber = member.WorkPhoneNumber ?? string.Empty,
                PreferredContactEmail = member.PreferredContactEmail ?? string.Empty,
                Address = member.Address ?? string.Empty,
                City = member.City ?? string.Empty,
                State = member.State ?? string.Empty,
                Zip = member.Zip ?? string.Empty,
                BirthDate = member.BirthDate,
                Sex = member.Sex ?? string.Empty,
                Comments = member.Comments ?? string.Empty,
                IsActive = member.IsActive,
                PrefersPhone = member.PrefersPhone,
                PrefersEmail = member.PrefersEmail,
                PrefersText = member.PrefersText
            };
        }
    }
}
