using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Data
{
    /// <summary>
    /// The member report's filters and sort, in one place.
    /// <para>
    /// This chain existed three times - <c>ReportsController.GetMemberReport</c>,
    /// <c>MemberReportModel.OnGetAsync</c> and <c>MemberReportModel.OnGetExport</c> - and the
    /// copies had already drifted: the export accepted <c>sortColumn</c> and
    /// <c>sortAscending</c> and then never sorted, so the CSV came out in database order while
    /// the screen showed the same filters sorted. That is the kind of difference three copies
    /// produce and no reviewer notices.
    /// </para>
    /// </summary>
    public static class MemberReportQuery
    {
        /// <summary>
        /// Every member the report can show, with the survey answers it displays, filtered by
        /// <paramref name="filter"/>.
        /// <para>
        /// Active members only. The report has never shown anyone else, which is why the CSV's
        /// Status column - computed after this filter - could only ever say "Active".
        /// </para>
        /// </summary>
        public static IQueryable<MemberInfo> Build(ApplicationDbContext context, MemberReportFilter filter)
        {
            IQueryable<MemberInfo> query = context.MemberInfos
                .Where(m => m.IsActive)
                .Include(m => m.MemberServiceRoles)
                .ThenInclude(msr => msr.InvolvementArea)
                .Include(m => m.MemberInterests)
                .ThenInclude(mi => mi.InterestArea)
                .Include(m => m.MemberInvolvements)
                .ThenInclude(mi => mi.InvolvementArea);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm;

                query = query.Where(m =>
                    (m.FirstName ?? "").Contains(term) ||
                    (m.LastName ?? "").Contains(term) ||
                    (m.Email ?? "").Contains(term) ||
                    (m.CellPhoneNumber ?? "").Contains(term));
            }

            if (filter.ServiceRoleIds.Count > 0)
            {
                var ids = filter.ServiceRoleIds;
                query = query.Where(m => m.MemberServiceRoles.Any(msr => ids.Contains(msr.InvolvementAreaID)));
            }

            if (filter.InterestIds.Count > 0)
            {
                var ids = filter.InterestIds;
                query = query.Where(m => m.MemberInterests.Any(mi => ids.Contains(mi.InterestAreaID)));
            }

            if (filter.InvolvementAreaIds.Count > 0)
            {
                var ids = filter.InvolvementAreaIds;
                query = query.Where(m => m.MemberInvolvements.Any(mi => ids.Contains(mi.InvolvementAreaID)));
            }

            return query;
        }

        /// <summary>
        /// Applies the report's sort. Name is the default and sorts on last name after first,
        /// so members sharing a first name come out in a stable order.
        /// </summary>
        public static IQueryable<MemberInfo> ApplySort(
            IQueryable<MemberInfo> query, string sortColumn, bool ascending) => sortColumn switch
            {
                "Email" => ascending
                    ? query.OrderBy(m => m.Email)
                    : query.OrderByDescending(m => m.Email),
                "Phone" => ascending
                    ? query.OrderBy(m => m.CellPhoneNumber)
                    : query.OrderByDescending(m => m.CellPhoneNumber),
                _ => ascending
                    ? query.OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
                    : query.OrderByDescending(m => m.FirstName).ThenByDescending(m => m.LastName)
            };

        /// <summary>
        /// Reads the comma-separated id lists the report passes in its query string, discarding
        /// anything that is not an integer.
        /// <para>
        /// One parse per value. The two copies this replaces both filtered with
        /// <c>int.TryParse</c> and then parsed the same string again with <c>int.Parse</c>.
        /// </para>
        /// </summary>
        public static List<int> ParseIntList(string? commaSeparatedValues)
        {
            if (string.IsNullOrEmpty(commaSeparatedValues))
            {
                return new List<int>();
            }

            var ids = new List<int>();

            foreach (var part in commaSeparatedValues.Split(','))
            {
                if (int.TryParse(part.Trim(), out var id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
    }

    /// <summary>What the report was asked to show. Ids are already parsed.</summary>
    public sealed class MemberReportFilter
    {
        public string SearchTerm { get; init; } = string.Empty;
        public List<int> ServiceRoleIds { get; init; } = new();
        public List<int> InterestIds { get; init; } = new();
        public List<int> InvolvementAreaIds { get; init; } = new();

        /// <summary>Builds a filter from the raw query-string values all three call sites receive.</summary>
        public static MemberReportFilter From(
            string? searchTerm, string? serviceRoles, string? interests, string? involvementAreas) => new()
            {
                SearchTerm = searchTerm ?? string.Empty,
                ServiceRoleIds = MemberReportQuery.ParseIntList(serviceRoles),
                InterestIds = MemberReportQuery.ParseIntList(interests),
                InvolvementAreaIds = MemberReportQuery.ParseIntList(involvementAreas)
            };
    }
}
