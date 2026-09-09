using System.Text.RegularExpressions;

namespace StewardshipSurvey.Tests.Unit
{
    /// <summary>
    /// Every <c>StaticResource</c> the MAUI client's XAML asks for must be registered in
    /// <c>App.xaml</c>.
    /// <para>
    /// An unresolved key is a *runtime* failure in MAUI, not a compile error. Three of them sat
    /// in this project for its whole life: it built clean and would have thrown on the login
    /// page, the first screen anyone sees. A clean build was never evidence to the contrary.
    /// </para>
    /// <para>
    /// This reads the XAML as text, so it needs neither the MAUI workload nor the project to be
    /// in the solution - it runs on the Linux CI box that cannot build MAUI at all. That is the
    /// point: it is the only automated check this client has.
    /// </para>
    /// </summary>
    public class MauiXamlResourceTests
    {
        private static readonly Regex StaticResourceUse =
            new(@"StaticResource\s+(?<key>[A-Za-z0-9_]+)", RegexOptions.Compiled);

        private static readonly Regex ResourceKey =
            new(@"x:Key=""(?<key>[A-Za-z0-9_]+)""", RegexOptions.Compiled);

        [Fact]
        public void Every_static_resource_the_maui_xaml_uses_is_registered()
        {
            var project = MauiProjectDirectory();
            var registered = ResourceKey
                .Matches(File.ReadAllText(Path.Combine(project, "App.xaml")))
                .Select(m => m.Groups["key"].Value)
                .ToHashSet();

            var unresolved = new List<string>();

            foreach (var file in XamlFiles(project))
            {
                foreach (Match use in StaticResourceUse.Matches(File.ReadAllText(file)))
                {
                    var key = use.Groups["key"].Value;
                    if (!registered.Contains(key))
                    {
                        unresolved.Add($"{Path.GetFileName(file)} -> {key}");
                    }
                }
            }

            Assert.True(unresolved.Count == 0,
                "These StaticResource keys are used but not registered in App.xaml, which would " +
                "throw when the page loads even though the project builds:" +
                Environment.NewLine + string.Join(Environment.NewLine, unresolved.Distinct()));
        }

        [Fact]
        public void The_xaml_this_guards_is_actually_being_found()
        {
            // A file-scanning test that silently matches nothing passes forever. If the client
            // is restructured this fails loudly rather than quietly covering it.
            var files = XamlFiles(MauiProjectDirectory()).Select(Path.GetFileName).ToList();

            Assert.Contains("LoginPage.xaml", files);
            Assert.Contains("MemberInfoPage.xaml", files);
            Assert.True(files.Count >= 6, $"Expected at least 6 XAML files, found {files.Count}.");
        }

        /// <summary>The client's own XAML, excluding build output.</summary>
        private static IEnumerable<string> XamlFiles(string project) =>
            Directory.EnumerateFiles(project, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                            && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

        /// <summary>
        /// Walks up from the test assembly to the folder holding the solution, then across to
        /// the MAUI project - which is deliberately not in that solution, so it cannot be found
        /// by project reference.
        /// </summary>
        private static string MauiProjectDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "StewardshipSurvey.sln")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);

            var project = Path.Combine(directory!.FullName, "StewardshipSurvey.Maui");
            Assert.True(Directory.Exists(project), $"MAUI project not found at {project}.");

            return project;
        }
    }
}
