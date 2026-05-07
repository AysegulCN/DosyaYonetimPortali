using System;
using System.Collections.Generic;

namespace DosyaYonetimPortali.MVC.Models
{
    public static class SystemLogger
    {
        public static List<LogViewModel> Logs { get; set; } = new List<LogViewModel>();
        public static List<LoginRecordViewModel> LoginRecords { get; set; } = new List<LoginRecordViewModel>();

        public static List<FileActivityViewModel> FileActivities { get; set; } = new List<FileActivityViewModel>
        {
            new FileActivityViewModel { Id = 1, FileName = "Q3_Maliyet_Analizi.pdf", FileExtension = "pdf", UserEmail = "aysegul@coredrive.com", ActionType = "İndirme", Date = "01.05.2026 14:12" },
            new FileActivityViewModel { Id = 2, FileName = "Logo_Yeni.png", FileExtension = "png", UserEmail = "patron@coredrive.com", ActionType = "Yükleme", Date = "01.05.2026 11:30" },
            new FileActivityViewModel { Id = 3, FileName = "Eski_Liste.xlsx", FileExtension = "xlsx", UserEmail = "aysegul@coredrive.com", ActionType = "Silme", Date = "30.04.2026 09:15" }
        };

        public static void AddLog(string level, string user, string description)
        {
            Logs.Insert(0, new LogViewModel { Id = Logs.Count + 1, Date = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss"), Level = level, User = user, Description = description });
        }

        public static void AddLoginRecord(string userEmail, string ipAddress, string browserDevice, string status, bool isSuccess)
        {
            LoginRecords.Insert(0, new LoginRecordViewModel { Id = LoginRecords.Count + 1, Date = DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss"), UserEmail = userEmail, IpAddress = ipAddress, BrowserDevice = browserDevice, Status = status, IsSuccess = isSuccess });
        }

        public static void AddFileActivity(string fileName, string fileExtension, string userEmail, string actionType)
        {
            FileActivities.Insert(0, new FileActivityViewModel { Id = FileActivities.Count + 1, FileName = fileName, FileExtension = fileExtension, UserEmail = userEmail, ActionType = actionType, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
        }

        public static void ClearLogs() => Logs.Clear();
    }
}