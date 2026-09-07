using StewardshipSurvey.Data;

namespace StewardshipSurvey.Tests.Unit
{
    /// <summary>
    /// Guards the shape of the role model. These are cheap, but they catch a role being added
    /// to the constants and forgotten in <see cref="Roles.All"/>, which would stop
    /// <c>AdminSeeder</c> ever creating it.
    /// </summary>
    public class RolesTests
    {
        [Theory]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Staff)]
        [InlineData(Roles.VolunteerOrganizer)]
        [InlineData(Roles.RegisteredUser)]
        [InlineData(Roles.Member)]
        [InlineData(Roles.ProspectiveMember)]
        public void All_contains_every_declared_role(string role)
        {
            Assert.Contains(role, Roles.All);
        }

        [Fact]
        public void Elevated_and_status_mirrored_roles_do_not_overlap()
        {
            // Deactivation strips Elevated and preserves StatusMirrored. If a role were in
            // both sets it would be stripped and kept at once.
            Assert.Empty(Roles.Elevated.Intersect(Roles.StatusMirrored));
        }

        [Fact]
        public void RegisteredUser_is_neither_elevated_nor_status_mirrored()
        {
            // It is the catch-all: granted to everyone, never stripped, never derived.
            Assert.DoesNotContain(Roles.RegisteredUser, Roles.Elevated);
            Assert.DoesNotContain(Roles.RegisteredUser, Roles.StatusMirrored);
        }

        [Fact]
        public void Elevated_and_status_mirrored_are_both_subsets_of_All()
        {
            Assert.All(Roles.Elevated, role => Assert.Contains(role, Roles.All));
            Assert.All(Roles.StatusMirrored, role => Assert.Contains(role, Roles.All));
        }

        [Fact]
        public void Role_names_are_unique()
        {
            Assert.Equal(Roles.All.Length, Roles.All.Distinct().Count());
        }
    }
}
