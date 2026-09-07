using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using Microsoft.EntityFrameworkCore;


namespace StewardshipSurvey.Pages.Staff
{
    [Authorize(Roles = "Staff,Admin")]
    public class MemberReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MemberReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MemberInfo> Members { get; set; } = new();
        public List<InvolvementAreas> ServiceRoles { get; set; } = new();
        public List<InterestAreas> Interests { get; set; } = new();
        public List<InvolvementAreas> InvolvementAreas { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;
        public string SortColumn { get; set; } = "FirstName";
        public bool SortAscending { get; set; } = true;

        public List<int> SelectedServiceRoles { get; set; } = new();
        public List<int> SelectedInterests { get; set; } = new();
        public List<int> SelectedInvolvementAreas { get; set; } = new();

        public async Task OnGetAsync(
            string searchTerm = "",
            string sortColumn = "FirstName",
            bool sortAscending = true,
            string serviceRoles = "",
            string interests = "",
            string involvementAreas = "")
        {
            SearchTerm = searchTerm;
            SortColumn = sortColumn;
            SortAscending = sortAscending;

            // Parse selected filters
            SelectedServiceRoles = ParseIntList(serviceRoles);
            SelectedInterests = ParseIntList(interests);
            SelectedInvolvementAreas = ParseIntList(involvementAreas);

            // Load filter options
            ServiceRoles = await _context.InvolvementAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.AreaOfInvolvement)
                .ToListAsync();

            Interests = await _context.InterestAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.InterestArea)
                .ToListAsync();

            InvolvementAreas = await _context.InvolvementAreas
                .Where(ia => ia.IsActive)
                .OrderBy(ia => ia.AreaOfInvolvement)
                .ToListAsync();

            // Get members with related data - use IQueryable, not IIncludableQueryable
            IQueryable<MemberInfo> query = _context.MemberInfos
                .Where(m => m.IsActive)
                .Include(m => m.MemberServiceRoles)
                .ThenInclude(msr => msr.InvolvementArea)
                .Include(m => m.MemberInterests)
                .ThenInclude(mi => mi.InterestArea)
                .Include(m => m.MemberInvolvements)
                .ThenInclude(mi => mi.InvolvementArea);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m =>
                    (m.FirstName ?? "").Contains(searchTerm) ||
                    (m.LastName ?? "").Contains(searchTerm) ||
                    (m.Email ?? "").Contains(searchTerm) ||
                    (m.CellPhoneNumber ?? "").Contains(searchTerm));
            }

            // Apply service roles filter
            if (SelectedServiceRoles.Any())
            {
                query = query.Where(m =>
                    m.MemberServiceRoles.Any(msr => SelectedServiceRoles.Contains(msr.InvolvementAreaID)));
            }

            // Apply interests filter
            if (SelectedInterests.Any())
            {
                query = query.Where(m =>
                    m.MemberInterests.Any(mi => SelectedInterests.Contains(mi.InterestAreaID)));
            }

            // Apply involvement areas filter
            if (SelectedInvolvementAreas.Any())
            {
                query = query.Where(m =>
                    m.MemberInvolvements.Any(mi => SelectedInvolvementAreas.Contains(mi.InvolvementAreaID)));
            }

            // Apply sorting
            Members = sortColumn switch
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
        }

        public IActionResult OnGetExport(
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

            // Use IQueryable instead of IIncludableQueryable
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

            var members = query.ToList();

            var csv = "First Name,Last Name,Email,Phone,Service Roles,Interests,Involvement Areas,Status\n";
            foreach (var member in members)
            {
                var serviceRolesList = string.Join("; ", member.MemberServiceRoles.Select(r => r.InvolvementArea?.AreaOfInvolvement ?? ""));
                var interestsList = string.Join("; ", member.MemberInterests.Select(i => i.InterestArea?.InterestArea ?? ""));
                var involvementsList = string.Join("; ", member.MemberInvolvements.Select(i => i.InvolvementArea?.AreaOfInvolvement ?? ""));
                var status = member.IsActive ? "Active" : "Inactive";

                csv += string.Join(",", new[]
                {
                    member.FirstName, member.LastName, member.Email, member.CellPhoneNumber,
                    serviceRolesList, interestsList, involvementsList, status
                }.Select(CsvField)) + "\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "MemberReport.csv");
        }

        /// <summary>
        /// Characters that make a spreadsheet read a cell as a formula rather than as text.
        /// Tab and carriage return are included because Excel skips them when deciding.
        /// </summary>
        private static readonly char[] FormulaTriggers = { '=', '+', '-', '@', '\t', '\r' };

        /// <summary>
        /// Renders one CSV field. Two separate jobs, and both are needed.
        /// <para>
        /// RFC 4180 quoting keeps the columns aligned: wrap in quotes, double any quote
        /// inside, so a name or comment containing a quote cannot break the row.
        /// </para>
        /// <para>
        /// Neutralising the first character stops Excel, Sheets and LibreOffice treating the
        /// value as a formula. Quoting alone does not help, because they strip the quotes
        /// before deciding. Without this, a member typing <c>=HYPERLINK(...)</c> into the
        /// Comments box gets it executed on a staff machine when the export is opened.
        /// </para>
        /// <para>
        /// The apostrophe shows up in some importers, which looks wrong on a phone number
        /// written as <c>+1 555 0100</c>. That is the accepted cost. The alternative is
        /// running whatever a member typed into a free-text box.
        /// </para>
        /// </summary>
        internal static string CsvField(string? value)
        {
            var text = value ?? string.Empty;

            // Leading whitespace does not protect the cell - the spreadsheet looks past it -
            // so the check has to look past it too, while the prefix still goes at the front.
            var significant = text.TrimStart();
            if (significant.Length > 0 && FormulaTriggers.Contains(significant[0]))
            {
                text = "'" + text;
            }

            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        internal List<int> ParseIntList(string commaSeparatedValues)
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
