using StewardshipSurvey.Data;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Tests.Unit
{
    public class MembershipStatusRolesTests
    {
        [Fact]
        public void RoleFor_maps_Member_to_the_Member_role()
        {
            Assert.Equal(Roles.Member, MembershipStatusRoles.RoleFor(MembershipStatus.Member));
        }

        [Fact]
        public void RoleFor_maps_ProspectiveMember_to_the_ProspectiveMember_role()
        {
            Assert.Equal(Roles.ProspectiveMember,
                MembershipStatusRoles.RoleFor(MembershipStatus.ProspectiveMember));
        }

        [Fact]
        public void RoleFor_maps_an_unanswered_status_to_no_role()
        {
            // Null means the person has not chosen yet, which must not silently become Member.
            Assert.Null(MembershipStatusRoles.RoleFor(null));
        }

        [Fact]
        public void RoleFor_only_ever_returns_a_status_mirrored_role()
        {
            foreach (var status in new MembershipStatus?[]
                     { MembershipStatus.Member, MembershipStatus.ProspectiveMember, null })
            {
                var role = MembershipStatusRoles.RoleFor(status);

                if (role != null)
                {
                    Assert.Contains(role, Roles.StatusMirrored);
                }
            }
        }
    }
}
