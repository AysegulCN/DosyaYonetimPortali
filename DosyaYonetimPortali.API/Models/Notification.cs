namespace DosyaYonetimPortali.API.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}