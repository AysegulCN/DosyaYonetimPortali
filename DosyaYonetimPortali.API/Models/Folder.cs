using System.IO.Compression;

namespace DosyaYonetimPortali.API.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        public int? ParentFolderId { get; set; }
        public Folder? ParentFolder { get; set; }
        public ICollection<Folder>? SubFolders { get; set; }

        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public ICollection<AppFile>? Files { get; set; }
    }
}