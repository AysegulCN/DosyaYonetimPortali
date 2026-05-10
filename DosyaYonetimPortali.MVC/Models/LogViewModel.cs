namespace DosyaYonetimPortali.MVC.Models
{
    public class LogViewModel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Level { get; set; }
        public string User { get; set; }
        public string Description { get; set; }
        public string Status { get; internal set; }
        public string UserEmail { get; internal set; }
        public string Message { get; internal set; }
    }
}