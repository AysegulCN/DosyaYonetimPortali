namespace DosyaYonetimPortali.API.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string? ActionType { get; set; } // Örn: Yükleme, Silme, İndirme
        public string? Details { get; set; } // Örn: "Ayşegül rapor.pdf dosyasını sildi"
        public DateTime LogDate { get; set; } = DateTime.Now;

        // İşlemi yapan kullanıcı
        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}