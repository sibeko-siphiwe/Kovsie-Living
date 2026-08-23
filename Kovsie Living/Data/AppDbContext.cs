using Kovsie_Living.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Student> Students { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<FinancialOfficer> FinancialOfficers { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Student>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasOne(s => s.User)
                      .WithOne(u => u.Student)
                      .HasForeignKey<Student>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Staff>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasOne(s => s.User)
                      .WithOne(u => u.Staff)
                      .HasForeignKey<Staff>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<FinancialOfficer>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.User)
                      .WithOne(u => u.FinancialOfficer)
                      .HasForeignKey<FinancialOfficer>(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Tariff>(entity =>
            {
                entity.HasKey(t => t.TariffId);
            });
        }
    }
}
