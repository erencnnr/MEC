using MEC.PDKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace MEC.PDKS.Data
{
    public class PdksDbContext : DbContext
    {
        public PdksDbContext(DbContextOptions<PdksDbContext> options)
            : base(options)
        {
        }

        public DbSet<PdksUser> PdksUsers { get; set; }
        public DbSet<PdksMovement> PdksMovements { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>().HaveMaxLength(255);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PdksUser>()
                .HasIndex(x => x.SicilNo)
                .IsUnique();

            modelBuilder.Entity<PdksUser>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<PdksUser>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<PdksMovement>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<PdksMovement>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<PdksMovement>()
                .HasIndex(x => x.PdksUserId);

            modelBuilder.Entity<PdksMovement>()
                .HasIndex(x => x.HareketZamani);

            modelBuilder.Entity<PdksMovement>()
                .HasIndex(x => new { x.PdksUserId, x.HareketZamani });

            modelBuilder.Entity<PdksMovement>()
                .Property(x => x.HareketTipi)
                .HasConversion<string>()
                .HasMaxLength(16);

            modelBuilder.Entity<PdksMovement>()
                .HasOne(x => x.PdksUser)
                .WithMany(x => x.Movements)
                .HasForeignKey(x => x.PdksUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
