using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;


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

            var filter = MemberReportFilter.From(searchTerm, serviceRoles, interests, involvementAreas);

            SelectedServiceRoles = filter.ServiceRoleIds;
            SelectedInterests = filter.InterestIds;
            SelectedInvolvementAreas = filter.InvolvementAreaIds;

            // Filter options for the form at the top of the page.
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

            var query = MemberReportQuery.ApplySort(
                MemberReportQuery.Build(_context, filter), sortColumn, sortAscending);

            Members = await query.ToListAsync();
        }

        public async Task<IActionResult> OnGetExportAsync(
            string searchTerm = "",
            string sortColumn = "FirstName",
            bool sortAscending = true,
            string serviceRoles = "",
            string interests = "",
            string involvementAreas = "")
        {
            var filter = MemberReportFilter.From(searchTerm, serviceRoles, interests, involvementAreas);

            // The sort used to be missing here entirely: this handler accepted sortColumn and
            // sortAscending and then materialised the query without an OrderBy, so the CSV came
            // out in whatever order the database returned while the screen above showed the
            // same filters sorted.
            var query = MemberReportQuery.ApplySort(
                MemberReportQuery.Build(_context, filter), sortColumn, sortAscending);

            var members = await query.ToListAsync();

            // StringBuilder, not repeated concatenation. A full export copied the entire CSV
            // once per member.
            var csv = new StringBuilder();

            // No Status column. It read member.IsActive, which the query above has already
            // filtered to true, so it said "Active" on every row of every export ever taken.
            csv.Append("First Name,Last Name,Email,Phone,Service Roles,Interests,Involvement Areas\n");

            foreach (var member in members)
            {
                var serviceRolesList = string.Join("; ", member.MemberServiceRoles.Select(r => r.InvolvementArea?.AreaOfInvolvement ?? ""));
                var interestsList = string.Join("; ", member.MemberInterests.Select(i => i.InterestArea?.InterestArea ?? ""));
                var involvementsList = string.Join("; ", member.MemberInvolvements.Select(i => i.InvolvementArea?.AreaOfInvolvement ?? ""));

                csv.Append(string.Join(",", new[]
                {
                    member.FirstName, member.LastName, member.Email, member.CellPhoneNumber,
                    serviceRolesList, interestsList, involvementsList
                }.Select(CsvField)));

                csv.Append('\n');
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
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

    }
}
