using StewardshipSurvey.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StewardshipSurvey.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<MemberInfo> MemberInfos { get; set; }
        public DbSet<InterestAreas> InterestAreas { get; set; }
        public DbSet<InvolvementAreas> InvolvementAreas { get; set; }
        //public DbSet<ServiceRoles> ServiceRoles { get; set; }
        public DbSet<MemberInterest> MemberInterests { get; set; }
        public DbSet<MemberInvolvement> MemberInvolvements { get; set; }
        public DbSet<MemberServiceRole> MemberServiceRoles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure a one-to-one relationship between ApplicationUser and MemberInfo
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(a => a.Member)
                .WithOne(m => m.ApplicationUser)
                .HasForeignKey<ApplicationUser>(a => a.MemberID);

            // Configure composite primary key for MemberInterest
            modelBuilder.Entity<MemberInterest>()
                .HasKey(mi => new { mi.MemberID, mi.InterestAreaID });
            // Configure relationships
            modelBuilder.Entity<MemberInterest>()
                .HasOne(mi => mi.Member)
                .WithMany(m => m.MemberInterests)
                .HasForeignKey(mi => mi.MemberID);
            modelBuilder.Entity<MemberInterest>()
                .HasOne(mi => mi.InterestArea)
                .WithMany(ia => ia.MemberInterests)
                .HasForeignKey(mi => mi.InterestAreaID);

            // Configure composite primary key for MemberInvolvement
            modelBuilder.Entity<MemberInvolvement>()
                .HasKey(mi => new { mi.MemberID, mi.InvolvementAreaID });
            // Configure relationships
            modelBuilder.Entity<MemberInvolvement>()
                .HasOne(mi => mi.Member)
                .WithMany(m => m.MemberInvolvements)
                .HasForeignKey(mi => mi.MemberID);
            modelBuilder.Entity<MemberInvolvement>()
                .HasOne(mi => mi.InvolvementArea)
                .WithMany(ia => ia.MemberInvolvements)
                .HasForeignKey(mi => mi.InvolvementAreaID);

            // Configure composite primary key for MemberServiceRole
            modelBuilder.Entity<MemberServiceRole>()
                .HasKey(msr => new { msr.MemberID, msr.InvolvementAreaID });
            // Configure relationships
            modelBuilder.Entity<MemberServiceRole>()
                .HasOne(msr => msr.Member)
                .WithMany(m => m.MemberServiceRoles)
                .HasForeignKey(msr => msr.MemberID);
            modelBuilder.Entity<MemberServiceRole>()
                .HasOne(msr => msr.InvolvementArea)
                .WithMany(m => m.MemberServiceRoles)
                .HasForeignKey(msr => msr.InvolvementAreaID);
        }
    }
}
