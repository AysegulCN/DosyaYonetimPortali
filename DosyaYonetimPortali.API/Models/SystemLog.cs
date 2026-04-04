namespace DosyaYonetimPortali.API.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string? ActionType { get; set; } 
        public string? Details { get; set; } 
        public DateTime LogDate { get; set; } = DateTime.Now;

        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}