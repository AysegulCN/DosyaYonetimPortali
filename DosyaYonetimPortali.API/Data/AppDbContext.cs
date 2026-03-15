using DosyaYonetimPortali.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DosyaYonetimPortali.API.Controllers.Data
{
    // IdentityDbContext'ten miras alıyoruz ki kullanıcı giriş/çıkış tabloları otomatik gelsin
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veri tabanında oluşacak tablolarımız
        public DbSet<Folder> Folders { get; set; }
        public DbSet<AppFile> Files { get; set; }

        // Veri tabanı çökmelerini önlemek için kuralları yazdığımız bölüm
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SQL Server'ın "döngüsel silme" hatası verip çökmesini engelliyoruz (Çok Önemli!)
            builder.Entity<Folder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.SubFolders)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict); // Bir klasör silinince altındakileri otomatik silmeye çalışıp kilitlenmesin
        }
    }
}