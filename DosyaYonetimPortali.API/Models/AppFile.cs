namespace DosyaYonetimPortali.API.Models
{
    public class AppFile
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public string? PhysicalPath { get; set; }
        public string? AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public long Size { get; set; } 
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public DateTime? ShareExpiration { get; set; } 
        public DateTime? DeletedDate { get; set; } 
        public int? FolderId { get; set; }
        public Folder Folder { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsStarred { get; set; } = false; 
        public Guid? ShareToken { get; set; } 
        
    }
}