namespace DosyaYonetimPortali.API.Models
{
    public class AppFile
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public string? PhysicalPath { get; set; }
        public long Size { get; set; } 
        public DateTime UploadDate { get; set; } = DateTime.Now;

        public int? FolderId { get; set; }
        public Folder Folder { get; set; }
        public bool IsDeleted { get; set; } = false; 
        public string? AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}