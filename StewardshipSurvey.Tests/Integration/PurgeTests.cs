using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Services;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// The retention sweep. These run against SQLite with foreign keys on, so the delete
    /// ordering is enforced by the database rather than assumed: AspNetUsers.MemberID is a
    /// NO ACTION foreign key, so the user row must be removed before the profile it points at.
    /// If that order ever regresses, <see cref="Purges_a_profile_and_every_related_row"/>
    /// fails with a constraint violation instead of passing quietly.
    /// </summary>
    public class PurgeTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public PurgeTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        private DeactivatedUserPurgeService CreateService(int retentionDays) =>
            new DeactivatedUserPurgeService(
                _factory.Services.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new UserRetentionOptions { PurgeDeactivatedAfterDays = retentionDays }),
                NullLogger<DeactivatedUserPurgeService>.Instance);

        [Fact]
        public async Task Purges_a_profile_and_every_related_row()
        {
            var email = $"purge-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var memberId = await GiveMemberSurveyAnswersAsync(user.Id);
            await DeactivateAsync(user.Id, DateTime.UtcNow.AddDays(-1200));

            await CreateService(1095).PurgeAsync(CancellationToken.None);

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                Assert.False(await context.Users.AnyAsync(u => u.Id == user.Id));
                Assert.False(await context.MemberInfos.AnyAsync(m => m.MemberID == memberId));
                Assert.False(await context.MemberInterests.AnyAsync(i => i.MemberID == memberId));
                Assert.False(await context.MemberInvolvements.AnyAsync(i => i.MemberID == memberId));
                Assert.False(await context.MemberServiceRoles.AnyAsync(s => s.MemberID == memberId));
            });
        }

        [Fact]
        public async Task Leaves_no_orphaned_profile_behind()
        {
            // Deleting only the user would strand the MemberInfo, and the Member Report joins
            // nothing, so an orphan keeps appearing on it forever.
            var email = $"orphan-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            await GiveMemberSurveyAnswersAsync(user.Id);
            await DeactivateAsync(user.Id, DateTime.UtcNow.AddDays(-1200));

            await CreateService(1095).PurgeAsync(CancellationToken.None);

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                var orphans = await context.MemberInfos
                    .Where(m => !context.Users.Any(u => u.MemberID == m.MemberID))
                    .CountAsync();

                Assert.Equal(0, orphans);
            });
        }

        [Fact]
        public async Task Keeps_an_account_still_inside_the_retention_window()
        {
            var email = $"recent-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email);
            await DeactivateAsync(user.Id, DateTime.UtcNow.AddDays(-10));

            await CreateService(1095).PurgeAsync(CancellationToken.None);

            await AssertStillExistsAsync(user.Id);
        }

        [Fact]
        public async Task Keeps_an_account_that_is_not_deactivated_at_all()
        {
            var email = $"active-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email);

            await CreateService(1095).PurgeAsync(CancellationToken.None);

            await AssertStillExistsAsync(user.Id);
        }

        [Fact]
        public async Task Never_purges_the_only_remaining_admin()
        {
            var email = $"lastadmin-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email, roles: Roles.Admin);
            await DeactivateAsync(user.Id, DateTime.UtcNow.AddDays(-1200));

            await CreateService(1095).PurgeAsync(CancellationToken.None);

            await AssertStillExistsAsync(user.Id);
        }

        /// <summary>Adds one row to each join table so the cascade has something to remove.</summary>
        private async Task<int> GiveMemberSurveyAnswersAsync(string userId)
        {
            var memberId = 0;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                var profile = await context.MemberInfos.FirstAsync(m => m.ApplicationUser!.Id == userId);
                memberId = profile.MemberID;

                var interest = new InterestAreas { InterestArea = $"Interest {Guid.NewGuid():N}", IsActive = true };
                var involvement = new InvolvementAreas { AreaOfInvolvement = $"Area {Guid.NewGuid():N}", IsActive = true };
                context.InterestAreas.Add(interest);
                context.InvolvementAreas.Add(involvement);
                await context.SaveChangesAsync();

                context.MemberInterests.Add(new MemberInterest
                {
                    MemberID = memberId,
                    InterestAreaID = interest.InterestAreaID
                });
                context.MemberInvolvements.Add(new MemberInvolvement
                {
                    MemberID = memberId,
                    InvolvementAreaID = involvement.InvolvementAreaID
                });
                context.MemberServiceRoles.Add(new MemberServiceRole
                {
                    MemberID = memberId,
                    InvolvementAreaID = involvement.InvolvementAreaID
                });
                await context.SaveChangesAsync();
            });

            return memberId;
        }

        private Task DeactivateAsync(string userId, DateTime deactivatedOn) =>
            _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var user = await context.Users.FirstAsync(u => u.Id == userId);
                user.DeactivatedDate = deactivatedOn;
                await context.SaveChangesAsync();
            });

        private Task AssertStillExistsAsync(string userId) =>
            _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                Assert.True(await context.Users.AnyAsync(u => u.Id == userId));
            });
    }
}
