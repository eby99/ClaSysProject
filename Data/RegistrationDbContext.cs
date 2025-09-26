using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Models;

namespace RegistrationPortal.Data
{
    public class RegistrationDbContext : DbContext
    {
        public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration to match existing database schema exactly
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserID);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                // SQLite compatible configuration
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DateOfBirth).IsRequired();
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.StreetAddress).HasMaxLength(200);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.ZipCode).HasMaxLength(20);
                entity.Property(e => e.SecurityQuestion).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SecurityAnswer).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Bio).HasMaxLength(500);
                entity.Property(e => e.ReferralCode).HasMaxLength(20);
                entity.Property(e => e.IPAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(255);
                
                // Bit fields with defaults
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.ReceiveNewsletter).HasDefaultValue(false);
                entity.Property(e => e.ReceiveSMS).HasDefaultValue(false);
                
                // DateTime fields with defaults (SQLite compatible)
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.LastModified).HasDefaultValueSql("datetime('now')");
            });

            // Admin entity configuration to match existing database schema exactly
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admins");
                entity.HasKey(e => e.AdminID);
                entity.HasIndex(e => e.Username).IsUnique();
                
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("datetime('now')");
            });
        }
    }
}