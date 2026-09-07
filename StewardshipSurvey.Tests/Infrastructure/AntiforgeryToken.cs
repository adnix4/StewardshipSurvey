using System.Text.RegularExpressions;

namespace StewardshipSurvey.Tests.Infrastructure
{
    /// <summary>
    /// Pulls the antiforgery token out of a rendered form. Every POST in this app goes through
    /// a Razor Pages form, so a test that posts without one gets a 400.
    /// </summary>
    public static class AntiforgeryToken
    {
        private static readonly Regex Pattern = new Regex(
            @"name=""__RequestVerificationToken""[^>]*value=""(?<token>[^""]+)""",
            RegexOptions.Compiled);

        public static string Extract(string html)
        {
            var match = Pattern.Match(html);

            Assert.True(match.Success,
                "No __RequestVerificationToken found in the page. Either the form did not " +
                "render or the request was redirected to somewhere without one.");

            return match.Groups["token"].Value;
        }
    }
}
