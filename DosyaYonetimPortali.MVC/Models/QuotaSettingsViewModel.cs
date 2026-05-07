namespace DosyaYonetimPortali.MVC.Models
{
    public class QuotaSettingsViewModel
    {
        public int DefaultUserQuotaGB { get; set; } = 15;
        public int MaxFileUploadMB { get; set; } = 2048;
        public int UserQuotaWarningPercent { get; set; } = 85;
        public int ServerCriticalAlarmPercent { get; set; } = 90;
    }
}