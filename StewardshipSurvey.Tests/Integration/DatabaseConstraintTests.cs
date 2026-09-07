using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;
using StewardshipSurvey.Tests.Infrastructure;

namespace StewardshipSurvey.Tests.Integration
{
    /// <summary>
    /// Proves the test database actually enforces referential integrity. Without this, the
    /// cascade assertions in <see cref="PurgeTests"/> could be satisfied by EF's in-memory
    /// fix-up rather than by the database, and would pass even against a schema that had lost
    /// its constraints.
    /// </summary>
    public class DatabaseConstraintTests : IClassFixture<StewardshipWebApplicationFactory>
    {
        private readonly StewardshipWebApplicationFactory _factory;

        public DatabaseConstraintTests(StewardshipWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Foreign_keys_are_enforced()
        {
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                var pragma = await context.Database
                    .SqlQueryRaw<long>("PRAGMA foreign_keys;")
                    .ToListAsync();

                Assert.Equal(1, pragma.Single());
            });
        }

        [Fact]
        public async Task A_survey_answer_cannot_reference_a_member_that_does_not_exist()
        {
            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                var interest = new InterestAreas { InterestArea = $"Orphan {Guid.NewGuid():N}", IsActive = true };
                context.InterestAreas.Add(interest);
                await context.SaveChangesAsync();

                context.MemberInterests.Add(new MemberInterest
                {
                    MemberID = 999_999,           // no such member
                    InterestAreaID = interest.InterestAreaID
                });

                await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
            });
        }

        [Fact]
        public async Task Deleting_a_profile_cascades_to_its_survey_answers_in_the_database()
        {
            // Deliberately done in two separate scopes so EF cannot cascade from tracked
            // children - the delete has to reach the database with nothing else loaded.
            var email = $"cascade-{Guid.NewGuid():N}@stmark.local";
            var user = await _factory.CreateUserAsync(email, status: MembershipStatus.Member);
            var memberId = 0;

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();
                var profile = await context.MemberInfos.FirstAsync(m => m.ApplicationUser!.Id == user.Id);
                memberId = profile.MemberID;

                var interest = new InterestAreas { InterestArea = $"Cascade {Guid.NewGuid():N}", IsActive = true };
                context.InterestAreas.Add(interest);
                await context.SaveChangesAsync();

                context.MemberInterests.Add(new MemberInterest
                {
                    MemberID = memberId,
                    InterestAreaID = interest.InterestAreaID
                });
                await context.SaveChangesAsync();
            });

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                // Break the user's link first, or the NO ACTION foreign key blocks the delete.
                var owner = await context.Users.FirstAsync(u => u.Id == user.Id);
                owner.MemberID = null;
                await context.SaveChangesAsync();

                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM MemberInfos WHERE MemberID = {0}", memberId);
            });

            await _factory.WithScopeAsync(async provider =>
            {
                var context = provider.GetRequiredService<ApplicationDbContext>();

                Assert.False(await context.MemberInterests.AnyAsync(i => i.MemberID == memberId));
            });
        }
    }
}
