using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortali.API.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Folder> Folders { get; set; }
        public DbSet<AppFile> Files { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<FileComment> FileComments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DosyaYonetimPortali.API.Models.FileShare> FileShares { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Folder>(entity =>
            {
                entity.HasOne(f => f.ParentFolder)
                      .WithMany(f => f.SubFolders)
                      .HasForeignKey(f => f.ParentFolderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(f => f.Files)
                      .WithOne(f => f.Folder)
                      .HasForeignKey(f => f.FolderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 2. EKLEME: Paylaşım İlişkisi Kuralları
            builder.Entity<DosyaYonetimPortali.API.Models.FileShare>()
                .HasOne(fs => fs.File)
                .WithMany(f => f.SharedWithUsers)
                .HasForeignKey(fs => fs.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                var updatedDateProp = entityEntry.Entity.GetType().GetProperty("UpdatedDate");
                if (updatedDateProp != null)
                {
                    updatedDateProp.SetValue(entityEntry.Entity, DateTime.Now);
                }

                if (entityEntry.State == EntityState.Added)
                {
                    var createdDateProp = entityEntry.Entity.GetType().GetProperty("CreatedDate");
                    if (createdDateProp != null)
                    {
                        createdDateProp.SetValue(entityEntry.Entity, DateTime.Now);
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}