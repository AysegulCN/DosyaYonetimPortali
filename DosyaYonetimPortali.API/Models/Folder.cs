using System.IO.Compression;

namespace DosyaYonetimPortali.API.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İç içe klasör mantığı için (Eğer ParentFolderId null ise ana dizindedir)
        public int? ParentFolderId { get; set; }
        public Folder ParentFolder { get; set; }
        public ICollection<Folder> SubFolders { get; set; }

        // Bu klasörü hangi kullanıcı oluşturdu?
        public string? AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        // Bu klasörün içindeki dosyalar
        public ICollection<AppFile> Files { get; set; }
    }
}