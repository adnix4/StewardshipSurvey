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
            var selectedServiceRoles = ParseIntList(serviceRoles);
            var selectedInterests = ParseIntList(interests);
            var selectedInvolvementAreas = ParseIntList(involvementAreas);

            IQueryable<MemberInfo> query = _context.MemberInfos
                .Where(m => m.IsActive)
                .Include(m => m.MemberServiceRoles)
                .ThenInclude(msr => msr.InvolvementArea)
                .Include(m => m.MemberInterests)
                .ThenInclude(mi => mi.InterestArea)
                .Include(m => m.MemberInvolvements)
                .ThenInclude(mi => mi.InvolvementArea);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m =>
                    (m.FirstName ?? "").Contains(searchTerm) ||
                    (m.LastName ?? "").Contains(searchTerm) ||
                    (m.Email ?? "").Contains(searchTerm) ||
                    (m.CellPhoneNumber ?? "").Contains(searchTerm));
            }

            if (selectedServiceRoles.Any())
            {
                query = query.Where(m =>
                    m.MemberServiceRoles.Any(msr => selectedServiceRoles.Contains(msr.InvolvementAreaID)));
            }

            if (selectedInterests.Any())
            {
                query = query.Where(m =>
                    m.MemberInterests.Any(mi => selectedInterests.Contains(mi.InterestAreaID)));
            }

            if (selectedInvolvementAreas.Any())
            {
                query = query.Where(m =>
                    m.MemberInvolvements.Any(mi => selectedInvolvementAreas.Contains(mi.InvolvementAreaID)));
            }

            var members = sortColumn switch
            {
                "Email" => sortAscending
                    ? await query.OrderBy(m => m.Email).ToListAsync()
                    : await query.OrderByDescending(m => m.Email).ToListAsync(),
                "Phone" => sortAscending
                    ? await query.OrderBy(m => m.CellPhoneNumber).ToListAsync()
                    : await query.OrderByDescending(m => m.CellPhoneNumber).ToListAsync(),
                _ => sortAscending
                    ? await query.OrderBy(m => m.FirstName).ThenBy(m => m.LastName).ToListAsync()
                    : await query.OrderByDescending(m => m.FirstName).ThenByDescending(m => m.LastName).ToListAsync()
            };

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

        private List<int> ParseIntList(string commaSeparatedValues)
        {
            if (string.IsNullOrEmpty(commaSeparatedValues))
                return new List<int>();

            return commaSeparatedValues.Split(',')
                .Where(s => int.TryParse(s.Trim(), out _))
                .Select(s => int.Parse(s.Trim()))
                .ToList();
        }
    }
}
