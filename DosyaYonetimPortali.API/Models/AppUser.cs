using Microsoft.AspNetCore.Identity;
using System.IO.Compression;

namespace DosyaYonetimPortali.API.Models
{
    // IdentityUser sınıfından miras alıyoruz ki hazır giriş/çıkış/şifreleme özellikleri gelsin
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Bir kullanıcının birden fazla klasörü ve dosyası olabilir (Bire-Çok İlişki)
        public ICollection<Folder> Folders { get; set; }
        public ICollection<AppFile> Files { get; set; }
    }
}