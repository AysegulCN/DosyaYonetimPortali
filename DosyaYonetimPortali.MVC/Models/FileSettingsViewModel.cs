namespace DosyaYonetimPortali.MVC.Models
{
    public class FileSettingsViewModel
    {
        public string AllowedExtensions { get; set; } = ".jpg, .png, .pdf, .docx, .xlsx, .zip, .rar, .mp4";
        public string BlockedExtensions { get; set; } = ".exe, .bat, .cmd, .sh, .js, .vbs";
        public bool AutoVirusScan { get; set; } = true;
        public bool OverwriteSameName { get; set; } = false;
    }
}