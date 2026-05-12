using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace DosyaYonetimPortali.MVC.Models
{
    public class UserDbModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public static class SystemLogger
    {
        public static List<LogViewModel> Logs { get; set; } = new List<LogViewModel>();
        public static List<FileActivityViewModel> FileActivities { get; set; } = new List<FileActivityViewModel>();
        public static List<LoginRecordViewModel> LoginRecords { get; set; } = new List<LoginRecordViewModel>();

        private static string usersDbPath = Path.Combine(Directory.GetCurrentDirectory(), "users_db.json");

        public static List<UserDbModel> GetUsers()
        {
            if (System.IO.File.Exists(usersDbPath))
            {
                var json = System.IO.File.ReadAllText(usersDbPath);
                return JsonSerializer.Deserialize<List<UserDbModel>>(json) ?? new List<UserDbModel>();
            }
            var defaultUsers = new List<UserDbModel>
            {
                new UserDbModel { Id = Guid.NewGuid().ToString().Substring(0, 8), FirstName = "Sistem", LastName = "Yöneticisi", Email = "patron@coredrive.com", Password = "123", Role = "Admin" },
                new UserDbModel { Id = Guid.NewGuid().ToString().Substring(0, 8), FirstName = "Ayşegül", LastName = "Yılmaz", Email = "aysegulcoban@gmail.com", Password = "123", Role = "Admin" }
            };
            SaveUsers(defaultUsers);
            return defaultUsers;
        }

        public static void SaveUsers(List<UserDbModel> users)
        {
            System.IO.File.WriteAllText(usersDbPath, JsonSerializer.Serialize(users));
        }

        public static void AddLog(string status, string userEmail, string message)
        {
            Logs.Insert(0, new LogViewModel { Status = status, UserEmail = userEmail, Message = message, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
        }

        public static void AddFileActivity(string fileName, string userEmail, string actionType)
        {
            FileActivities.Insert(0, new FileActivityViewModel { FileName = fileName, UserEmail = userEmail, ActionType = actionType, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
            AddLog("Başarılı", userEmail, $"'{fileName}' üzerinde işlem: {actionType}");
        }

        public static void AddLoginRecord(string email, string ipAddress, string browserInfo, string status, bool isSuccess)
        {
            LoginRecords.Insert(0, new LoginRecordViewModel { UserEmail = email, IpAddress = ipAddress, BrowserDevice = browserInfo, Status = status, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
        }

        public static void ClearLogs()
        {
            Logs.Clear();
        }
    }
}