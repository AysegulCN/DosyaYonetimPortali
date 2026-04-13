namespace DosyaYonetimPortali.API.Models
{
    public class FileShare
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public AppFile? File { get; set; }

        public string? SharedWithUserId { get; set; }
        public AppUser? SharedWithUser { get; set; }

        public bool CanEdit { get; set; }
        public DateTime SharedDate { get; set; } = DateTime.Now;
    }
}