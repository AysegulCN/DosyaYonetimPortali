namespace DosyaYonetimPortali.API.Models
{
    public class FileComment
    {
        public int Id { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int AppFileId { get; set; }
        public AppFile? AppFile { get; set; }

        public string AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}