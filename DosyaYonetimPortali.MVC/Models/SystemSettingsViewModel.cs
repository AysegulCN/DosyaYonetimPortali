namespace DosyaYonetimPortali.MVC.Models
{
    public class SystemSettingsViewModel
    {
        public int MaxFileSizeMB { get; set; } = 2048;
        public int DefaultUserQuotaGB { get; set; } = 15;
        public bool AllowMediaFiles { get; set; } = true;
        public bool AllowArchiveFiles { get; set; } = true;
        public bool AllowExecutableFiles { get; set; } = false;
    }
}