using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Models.DTOs;

namespace StewardshipSurvey.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Staff,Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("members")]
        public async Task<ActionResult<List<MemberReportDto>>> GetMemberReport(
            string searchTerm = "",
            string sortColumn = "FirstName",
            bool sortAscending = true,
            string serviceRoles = "",
            string interests = "",
            string involvementAreas = "")
        {
            var filter = MemberReportFilter.From(searchTerm, serviceRoles, interests, involvementAreas);

            var query = MemberReportQuery.ApplySort(
                MemberReportQuery.Build(_context, filter), sortColumn, sortAscending);

            var members = await query.ToListAsync();

            var report = members.Select(m => new MemberReportDto
            {
                MemberID = m.MemberID,
                FirstName = m.FirstName ?? string.Empty,
                LastName = m.LastName ?? string.Empty,
                Email = m.Email ?? string.Empty,
                CellPhoneNumber = m.CellPhoneNumber ?? string.Empty,
                IsActive = m.IsActive,
                ServiceRoles = m.MemberServiceRoles.Select(r => r.InvolvementArea?.AreaOfInvolvement ?? string.Empty).ToList(),
                Interests = m.MemberInterests.Select(i => i.InterestArea?.InterestArea ?? string.Empty).ToList(),
                InvolvementAreas = m.MemberInvolvements.Select(i => i.InvolvementArea?.AreaOfInvolvement ?? string.Empty).ToList()
            }).ToList();

            return Ok(report);
        }

    }
}
