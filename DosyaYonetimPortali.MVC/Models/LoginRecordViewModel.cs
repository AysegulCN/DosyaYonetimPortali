namespace DosyaYonetimPortali.MVC.Models
{
    public class LoginRecordViewModel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string UserEmail { get; set; }
        public string IpAddress { get; set; }
        public string BrowserDevice { get; set; }
        public string Status { get; set; }
        public bool IsSuccess { get; set; }
    }
}