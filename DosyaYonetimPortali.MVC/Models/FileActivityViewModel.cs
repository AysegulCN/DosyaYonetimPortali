namespace DosyaYonetimPortali.MVC.Models
{
    public class FileActivityViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string UserEmail { get; set; }
        public string ActionType { get; set; }
        public string Date { get; set; }
    }
}