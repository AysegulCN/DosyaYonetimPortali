using DosyaYonetimPortali.MVC.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DosyaYonetimPortali.MVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DriveItem> DriveItems { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<LoginRecord> LoginRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(u => u.Email).IsUnique(); 
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<DriveItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Name).IsRequired().HasMaxLength(255);

                entity.HasOne<DriveItem>()
                      .WithMany()
                      .HasForeignKey(i => i.ParentId)
                      .OnDelete(DeleteBehavior.Restrict); 

                entity.Property(i => i.OwnerEmail).IsRequired();
            });

            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.Property(l => l.Message).IsRequired();
                entity.Property(l => l.Date).HasMaxLength(30);
            });
        }

        public override int SaveChanges()
        {
            ApplyTracking();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTracking();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTracking()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var modifiedProp = entry.Entity.GetType().GetProperty("ModifiedDate");
                if (modifiedProp != null)
                {
                    modifiedProp.SetValue(entry.Entity, DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                }

                if (entry.State == EntityState.Added)
                {
                    var dateProp = entry.Entity.GetType().GetProperty("Date");
                    if (dateProp != null)
                    {
                        dateProp.SetValue(entry.Entity, DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    }
                }
            }
        }
    }
}