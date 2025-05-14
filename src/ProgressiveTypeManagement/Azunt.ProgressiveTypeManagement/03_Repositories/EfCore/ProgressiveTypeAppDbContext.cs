using Microsoft.EntityFrameworkCore;

namespace Azunt.ProgressiveTypeManagement
{
    public class ProgressiveTypeAppDbContext : DbContext
    {
        public ProgressiveTypeAppDbContext(DbContextOptions<ProgressiveTypeAppDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgressiveType>()
                .Property(m => m.Created)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<ProgressiveType> ProgressiveTypes { get; set; } = null!;
    }
}