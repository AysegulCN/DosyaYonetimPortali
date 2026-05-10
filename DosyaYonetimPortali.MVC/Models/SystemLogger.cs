using System;
using System.Collections.Generic;
using System.Linq;

namespace DosyaYonetimPortali.MVC.Models
{
    public static class SystemLogger
    {
        public static List<LogViewModel> Logs { get; set; } = new List<LogViewModel>();
        public static List<FileActivityViewModel> FileActivities { get; set; } = new List<FileActivityViewModel>();
        public static List<LoginRecordViewModel> LoginRecords { get; set; } = new List<LoginRecordViewModel>();

        public static void AddLog(string status, string userEmail, string message)
        {
            Logs.Insert(0, new LogViewModel
            {
                Status = status,
                UserEmail = userEmail,
                Message = message,
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
        }

        public static void AddFileActivity(string fileName, string userEmail, string actionType)
        {
            FileActivities.Insert(0, new FileActivityViewModel
            {
                FileName = fileName,
                UserEmail = userEmail,
                ActionType = actionType,
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            AddLog("Başarılı", userEmail, $"'{fileName}' üzerinde işlem: {actionType}");
        }

        public static void AddLoginRecord(string email, string ipAddress, string browserInfo, string status, bool isSuccess)
        {
            LoginRecords.Insert(0, new LoginRecordViewModel
            {
                UserEmail = email,
                IpAddress = ipAddress,
                BrowserDevice = browserInfo,
                Status = status,
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
        }

        public static void ClearLogs()
        {
            Logs.Clear();
        }
    }
}