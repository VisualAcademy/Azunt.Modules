using Microsoft.EntityFrameworkCore;

namespace Azunt.DenominationManagement
{
    public class DenominationAppDbContext : DbContext
    {
        public DenominationAppDbContext(DbContextOptions<DenominationAppDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Denomination>()
                .Property(m => m.Created)
                .HasDefaultValueSql("GetDate()");
        }

        public DbSet<Denomination> Denominations { get; set; } = null!;
    }
}