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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // KLASÖR İLİŞKİLERİ: Kendi kendine referans (Self-Referencing)
            builder.Entity<Folder>(entity =>
            {
                entity.HasOne(f => f.ParentFolder)
                      .WithMany(f => f.SubFolders)
                      .HasForeignKey(f => f.ParentFolderId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Klasör silindiğinde içindeki dosyaların silinme davranışını belirleyebilirsin
                entity.HasMany(f => f.Files)
                      .WithOne(f => f.Folder)
                      .HasForeignKey(f => f.FolderId)
                      .OnDelete(DeleteBehavior.SetNull); // Klasör silinirse dosyalar "Root"a düşsün
            });
        }

        // HOCANIN EN ÇOK SEVECEĞİ KISIM: Otomatik Tarih Yönetimi
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                // Eğer modelde UpdatedDate varsa otomatik güncelle
                var updatedDateProp = entityEntry.Entity.GetType().GetProperty("UpdatedDate");
                if (updatedDateProp != null)
                {
                    updatedDateProp.SetValue(entityEntry.Entity, DateTime.Now);
                }

                // Eğer yeni ekleniyorsa ve CreatedDate varsa otomatik set et
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