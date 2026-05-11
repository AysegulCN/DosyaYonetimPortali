using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly string profileDbPath = Path.Combine(Directory.GetCurrentDirectory(), "admin_profile.json");

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            ViewBag.PendingRequestGB = DriveController.PendingQuotaRequestGB;

            int totalFiles = 0;
            int activeShares = 0;
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");

            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                totalFiles = db.Count(i => !i.IsFolder && !i.IsDeleted);
                activeShares = db.Count(i => i.IsShared && !i.IsDeleted);
            }

            var storageInfo = DriveController.GetStorageInfo();

            ViewBag.TotalUsers = _tempUsers.Count;
            ViewBag.TotalFiles = totalFiles;
            ViewBag.ActiveShares = activeShares;
            ViewBag.StoragePercent = storageInfo.Percent;

            var recentLogs = SystemLogger.Logs != null
                             ? SystemLogger.Logs.OrderByDescending(l => l.Date).Take(6).ToList()
                             : new List<LogViewModel>();

            return View(recentLogs);
        }

        [HttpGet]
        public IActionResult Storage()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");
            long usedBytes = 0;
            long mediaBytes = 0;
            long docBytes = 0;
            long archiveBytes = 0;

            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();

                var activeFiles = db.Where(i => !i.IsFolder && !i.IsDeleted).ToList();
                usedBytes = activeFiles.Sum(i => i.SizeBytes);

                mediaBytes = activeFiles.Where(i => new[] { "jpg", "jpeg", "png", "gif", "mp4", "mov" }.Contains(i.Extension?.ToLower())).Sum(i => i.SizeBytes);
                docBytes = activeFiles.Where(i => new[] { "pdf", "doc", "docx", "xls", "xlsx", "txt" }.Contains(i.Extension?.ToLower())).Sum(i => i.SizeBytes);
                archiveBytes = activeFiles.Where(i => new[] { "zip", "rar", "7z", "tar" }.Contains(i.Extension?.ToLower())).Sum(i => i.SizeBytes);
            }

            long totalBytes = DriveController.UserTotalQuotaMB * 1048576;

            ViewBag.UsedGB = (usedBytes / 1073741824.0).ToString("F2");
            ViewBag.TotalGB = (totalBytes / 1073741824.0).ToString("F2");
            ViewBag.StoragePercent = totalBytes > 0 ? (int)((usedBytes * 100) / totalBytes) : 0;

            ViewBag.MediaGB = (mediaBytes / 1073741824.0).ToString("F2");
            ViewBag.DocGB = (docBytes / 1073741824.0).ToString("F2");
            ViewBag.ArchiveGB = (archiveBytes / 1073741824.0).ToString("F2");

            ViewBag.MediaPercent = usedBytes > 0 ? (int)((mediaBytes * 100) / usedBytes) : 0;
            ViewBag.DocPercent = usedBytes > 0 ? (int)((docBytes * 100) / usedBytes) : 0;
            ViewBag.ArchivePercent = usedBytes > 0 ? (int)((archiveBytes * 100) / usedBytes) : 0;

            return View();
        }

        [HttpGet]
        public IActionResult Logs()
        {
            return View(SystemLogger.Logs);
        }

        [HttpPost]
        public IActionResult ClearOldLogs()
        {
            SystemLogger.ClearLogs();
            SystemLogger.AddLog("SYSTEM", "Sistem Yöneticisi", "Tüm eski log kayıtları manuel olarak temizlendi.");
            TempData["ToastMessage"] = "Sistemdeki tüm eski log kayıtları başarıyla temizlendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Logs");
        }

        [HttpGet]
        public IActionResult Shares()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");
            var sharedFiles = new List<DriveItemViewModel>();

            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                sharedFiles = db.Where(i => i.IsShared && !i.IsDeleted).ToList();
            }

            return View(sharedFiles);
        }

        [HttpPost]
        public IActionResult RevokeShare(string id)
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");
            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                var item = db.FirstOrDefault(i => i.Id == id);
                if (item != null)
                {
                    item.IsShared = false;
                    item.SharedWith = null;
                    System.IO.File.WriteAllText(dbPath, JsonSerializer.Serialize(db));
                    SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{item.Name}' dosyasının paylaşımı iptal edildi.");
                }
            }
            TempData["Message"] = "Seçilen dosyanın paylaşım bağlantısı başarıyla iptal edildi.";
            return RedirectToAction("Shares");
        }

        [HttpPost]
        public IActionResult RevokeAllShares()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");
            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                foreach (var item in db.Where(i => i.IsShared))
                {
                    item.IsShared = false;
                    item.SharedWith = null;
                }
                System.IO.File.WriteAllText(dbPath, JsonSerializer.Serialize(db));
                SystemLogger.AddLog("WARN", "Sistem Yöneticisi", "Sistemdeki tüm açık paylaşım bağlantıları kapatıldı.");
            }
            TempData["Message"] = "Sistemdeki tüm açık paylaşım bağlantıları başarıyla kapatıldı.";
            return RedirectToAction("Shares");
        }

        private static SystemSettingsViewModel _systemSettings = new SystemSettingsViewModel();

        [HttpGet]
        public IActionResult Settings()
        {
            return View(_systemSettings);
        }

        [HttpPost]
        public IActionResult SaveSettings(SystemSettingsViewModel model)
        {
            _systemSettings = model;
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Sistem yükleme limitleri ve güvenlik ayarları güncellendi.");
            TempData["ToastMessage"] = "Sistem ve güvenlik ayarları başarıyla kaydedildi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Settings");
        }

        private static List<UserViewModel> _tempUsers = new List<UserViewModel>
        {
            new UserViewModel { Id = "1", FirstName = "Ayşegül", LastName = "Yılmaz", Email = "aysegul@coredrive.com", Role = "User" },
            new UserViewModel { Id = "2", FirstName = "Sistem", LastName = "Yöneticisi", Email = "patron@coredrive.com", Role = "Admin" }
        };

        [HttpGet]
        public IActionResult Users()
        {
            return View(_tempUsers);
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(UserViewModel model)
        {
            model.Id = Guid.NewGuid().ToString().Substring(0, 8);
            _tempUsers.Add(model);
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{model.FirstName} {model.LastName}' isimli yeni bir kullanıcı sisteme eklendi.");
            TempData["ToastMessage"] = $"{model.FirstName} {model.LastName} isimli kullanıcı ({model.Role}) olarak sisteme başarıyla eklendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult DeleteUser(string id)
        {
            var user = _tempUsers.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _tempUsers.Remove(user);
                SystemLogger.AddLog("WARN", "Sistem Yöneticisi", $"'{user.FirstName} {user.LastName}' isimli kullanıcı sistemden silindi.");
                TempData["ToastMessage"] = "Kullanıcı sistemden kalıcı olarak silindi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult EditUser(string Id, string FirstName, string LastName, string Role)
        {
            var user = _tempUsers.FirstOrDefault(u => u.Id == Id);
            if (user != null)
            {
                user.FirstName = FirstName;
                user.LastName = LastName;
                user.Role = Role;
                SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{FirstName} {LastName}' kullanıcısının bilgileri güncellendi.");
                TempData["ToastMessage"] = "Kullanıcı bilgileri başarıyla güncellendi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Users");
        }

        private static List<RoleViewModel> _tempRoles = new List<RoleViewModel>
        {
             new RoleViewModel { RoleName = "Admin", UserCount = 2, IsSystemRole = true },
             new RoleViewModel { RoleName = "User", UserCount = 122, IsSystemRole = false }
        };

        [HttpGet]
        public IActionResult Roles()
        {
            return View(_tempRoles);
        }

        [HttpPost]
        public IActionResult AddRole(string RoleName)
        {
            if (!string.IsNullOrEmpty(RoleName) && !_tempRoles.Any(r => r.RoleName.ToLower() == RoleName.ToLower()))
            {
                _tempRoles.Add(new RoleViewModel { RoleName = RoleName, UserCount = 0, IsSystemRole = false });
                foreach (var perm in _tempPermissions)
                {
                    if (perm.RoleAccesses == null) perm.RoleAccesses = new List<RoleAccessViewModel>();
                    perm.RoleAccesses.Add(new RoleAccessViewModel { RoleName = RoleName, HasAccess = false });
                }
                SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{RoleName}' isimli yeni sistem rolü oluşturuldu.");
                TempData["ToastMessage"] = $"'{RoleName}' rolü sisteme başarıyla eklendi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public IActionResult EditRole(string OldRoleName, string NewRoleName)
        {
            var role = _tempRoles.FirstOrDefault(r => r.RoleName == OldRoleName);
            if (role != null && !role.IsSystemRole)
            {
                role.RoleName = NewRoleName;
                foreach (var perm in _tempPermissions)
                {
                    var roleAccess = perm.RoleAccesses?.FirstOrDefault(ra => ra.RoleName == OldRoleName);
                    if (roleAccess != null)
                    {
                        roleAccess.RoleName = NewRoleName;
                    }
                }
                SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{OldRoleName}' rolünün adı '{NewRoleName}' olarak değiştirildi.");
                TempData["ToastMessage"] = $"Rol adı '{NewRoleName}' olarak güncellendi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public IActionResult DeleteRole(string RoleName)
        {
            var role = _tempRoles.FirstOrDefault(r => r.RoleName == RoleName);
            if (role != null && !role.IsSystemRole)
            {
                _tempRoles.Remove(role);
                foreach (var perm in _tempPermissions)
                {
                    perm.RoleAccesses?.RemoveAll(ra => ra.RoleName == RoleName);
                }
                SystemLogger.AddLog("WARN", "Sistem Yöneticisi", $"'{RoleName}' isimli sistem rolü silindi.");
                TempData["ToastMessage"] = $"'{RoleName}' rolü sistemden kalıcı olarak silindi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Roles");
        }

        private static PermissionViewModel CreatePermission(string name, bool isCore)
        {
            var perm = new PermissionViewModel { ModuleName = name, IsCore = isCore, RoleAccesses = new List<RoleAccessViewModel>() };
            foreach (var role in _tempRoles)
            {
                perm.RoleAccesses.Add(new RoleAccessViewModel { RoleName = role.RoleName, HasAccess = role.RoleName == "Admin" });
            }
            return perm;
        }

        private static List<PermissionViewModel> _tempPermissions = new List<PermissionViewModel>
        {
            CreatePermission("Dosya Yükleme ve İndirme", true),
            CreatePermission("Klasör Oluşturma ve Hiyerarşi Yönetimi", false),
            CreatePermission("Dosya Silme (Kalıcı Silme)", true),
            CreatePermission("Çöp Kutusu Yönetimi (Geri Getirme)", false),
            CreatePermission("Dışarıya Açık Paylaşım (Link Oluşturma)", false),
            CreatePermission("Paylaşım Bağlantılarına Şifre ve Süre Koyma", false),
            CreatePermission("Ortak Çalışma Klasörleri (Workspace) Oluşturma", false),
            CreatePermission("Dosya Versiyon Geçmişi (Sürüm Kontrolü)", false),
            CreatePermission("Dosya İçi Yorum Yapma ve Etiketleme", false),
            CreatePermission("Sistem ve Kullanıcı Yönetimi", true),
            CreatePermission("Depolama Kotası Belirleme ve Yönetme", true)
        };

        [HttpGet]
        public IActionResult Permissions()
        {
            ViewBag.Roles = _tempRoles;
            return View(_tempPermissions);
        }

        [HttpPost]
        public IActionResult SavePermissions(List<PermissionViewModel> permissions)
        {
            if (permissions != null && permissions.Any())
            {
                _tempPermissions = permissions;
                foreach (var p in _tempPermissions)
                {
                    if (p.IsCore)
                    {
                        var adminAccess = p.RoleAccesses?.FirstOrDefault(r => r.RoleName == "Admin");
                        if (adminAccess != null) adminAccess.HasAccess = true;
                    }
                }
            }
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Sistem yetki matrisi ve rol izinleri güncellendi.");
            TempData["ToastMessage"] = "Tüm yetki ayarları ve modüller başarıyla güncellendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Permissions");
        }

        [HttpPost]
        public IActionResult AddPermission(string ModuleName, string ModuleType)
        {
            if (!string.IsNullOrWhiteSpace(ModuleName) && !_tempPermissions.Any(p => p.ModuleName.Equals(ModuleName, StringComparison.OrdinalIgnoreCase)))
            {
                bool isCore = ModuleType == "Core";
                var newPerm = new PermissionViewModel { ModuleName = ModuleName, IsCore = isCore, RoleAccesses = new List<RoleAccessViewModel>() };
                foreach (var role in _tempRoles)
                {
                    newPerm.RoleAccesses.Add(new RoleAccessViewModel { RoleName = role.RoleName, HasAccess = role.RoleName == "Admin" });
                }
                _tempPermissions.Add(newPerm);
                SystemLogger.AddLog("INFO", "Sistem Yöneticisi", $"'{ModuleName}' modülü için yeni sistem yetkisi eklendi.");
                TempData["ToastMessage"] = $"Yeni yetki modülü ({ModuleName}) sisteme başarıyla entegre edildi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Permissions");
        }

        [HttpPost]
        public IActionResult DeletePermission(string ModuleName)
        {
            var permission = _tempPermissions.FirstOrDefault(p => p.ModuleName == ModuleName);
            if (permission != null)
            {
                _tempPermissions.Remove(permission);
                SystemLogger.AddLog("WARN", "Sistem Yöneticisi", $"'{ModuleName}' modülü sistem yetki matrisinden silindi.");
                TempData["ToastMessage"] = $"'{ModuleName}' modülü sistemden kalıcı olarak silindi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Permissions");
        }

        [HttpGet]
        public IActionResult LoginRecords()
        {
            return View(SystemLogger.LoginRecords);
        }

        [HttpPost]
        public IActionResult DownloadLoginRecordsCsv()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Tarih/Saat,Kullanici,IP Adresi,Tarayici/Cihaz,Durum");
            foreach (var record in SystemLogger.LoginRecords)
            {
                builder.AppendLine($"{record.Date},{record.UserEmail},{record.IpAddress},{record.BrowserDevice},{record.Status}");
            }
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Kullanıcı giriş kayıtları CSV olarak indirildi.");
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "GirisKayitlari.csv");
        }

        [HttpGet]
        public IActionResult FileActivities()
        {
            return View(SystemLogger.FileActivities);
        }

        [HttpPost]
        public IActionResult DownloadFileActivitiesCsv()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Dosya Adi,Islem Yapan,Aksiyon,Tarih");
            foreach (var activity in SystemLogger.FileActivities)
            {
                builder.AppendLine($"{activity.FileName},{activity.UserEmail},{activity.ActionType},{activity.Date}");
            }
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Dosya hareketleri CSV olarak indirildi.");
            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "DosyaHareketleri.csv");
        }

        private static FileSettingsViewModel _fileSettings = new FileSettingsViewModel();
        private static QuotaSettingsViewModel _quotaSettings = new QuotaSettingsViewModel();

        [HttpGet]
        public IActionResult FileSettings()
        {
            return View(_fileSettings);
        }

        [HttpPost]
        public IActionResult SaveFileSettings(FileSettingsViewModel model)
        {
            _fileSettings = model;
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Dosya izinleri ve güvenlik ayarları güncellendi.");
            TempData["ToastMessage"] = "Dosya güvenlik ayarları başarıyla kaydedildi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("FileSettings");
        }

        [HttpGet]
        public IActionResult QuotaManagement()
        {
            ViewBag.CurrentQuotaMB = DriveController.UserTotalQuotaMB;
            ViewBag.PendingRequestGB = DriveController.PendingQuotaRequestGB;
            return View(_quotaSettings);
        }

        [HttpPost]
        public IActionResult ApproveQuotaRequest()
        {
            if (DriveController.PendingQuotaRequestGB > 0)
            {
                DriveController.UserTotalQuotaMB = DriveController.PendingQuotaRequestGB * 1024;
                DriveController.PendingQuotaRequestGB = 0;
                SystemLogger.AddLog("Başarılı", "Sistem", "Kullanıcının kota artırım talebi onaylandı.");
                TempData["ToastMessage"] = "Kullanıcının kota talebi başarıyla ONAYLANDI.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult RejectQuotaRequest()
        {
            DriveController.PendingQuotaRequestGB = 0;
            SystemLogger.AddLog("WARN", "Sistem", "Kullanıcının kota artırım talebi reddedildi.");
            TempData["ToastMessage"] = "Kullanıcının kota talebi REDDEDİLDİ.";
            TempData["ToastIcon"] = "error";
            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult ForceSetQuota(int targetGB)
        {
            DriveController.UserTotalQuotaMB = (long)targetGB * 1024;
            SystemLogger.AddLog("Admin", "Sistem", $"Kullanıcı kotası zorla {targetGB} GB olarak ayarlandı.");
            TempData["ToastMessage"] = $"Kullanıcının kotası zorla {targetGB} GB seviyesine ayarlandı.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult SaveQuotaSettings(QuotaSettingsViewModel model)
        {
            _quotaSettings = model;
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Sistem kota limitleri güncellendi.");
            TempData["ToastMessage"] = "Sistem kota ayarları başarıyla kaydedildi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult RefreshStorage()
        {
            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Sunucu depolama verileri manuel olarak yenilendi.");
            TempData["Message"] = "Sunucu depolama verileri güncellendi.";
            return RedirectToAction("Storage");
        }

        [HttpPost]
        public IActionResult GenerateSystemReport()
        {
            int totalUsers = _tempUsers.Count;
            int totalFiles = 0;
            int activeShares = 0;
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");

            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                totalFiles = db.Count(i => !i.IsFolder && !i.IsDeleted);
                activeShares = db.Count(i => i.IsShared && !i.IsDeleted);
            }
            var storageInfo = DriveController.GetStorageInfo();

            SystemLogger.AddLog("INFO", "Sistem Yöneticisi", "Genel sistem durumu PDF raporu oluşturuldu.");
            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                document.Add(new Paragraph("CORE-DRIVE SISTEM RAPORU").SetTextAlignment(TextAlignment.CENTER).SetFontSize(22));
                document.Add(new Paragraph("Rapor Tarihi: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));
                document.Add(new Paragraph("---------------------------------------------------------------------------------------------------"));
                document.Add(new Paragraph($"Toplam Kullanici: {totalUsers}").SetFontSize(14));
                document.Add(new Paragraph($"Yuklenen Toplam Dosya: {totalFiles}").SetFontSize(14));
                document.Add(new Paragraph($"Aktif Paylasilan Linkler: {activeShares}").SetFontSize(14));
                document.Add(new Paragraph($"Sistem Depolama Dolulugu: %{storageInfo.Percent}").SetFontSize(14));
                document.Add(new Paragraph("---------------------------------------------------------------------------------------------------"));
                document.Add(new Paragraph("Sistem Durumu: SAGLIKLI").SetFontSize(12));
                document.Close();
                return File(ms.ToArray(), "application/pdf", $"CoreDrive_Rapor_{DateTime.Now.ToString("yyyyMMdd")}.pdf");
            }
        }

        public static AdminProfileData GetAdminProfileData()
        {
            if (System.IO.File.Exists(profileDbPath))
            {
                var json = System.IO.File.ReadAllText(profileDbPath);
                return JsonSerializer.Deserialize<AdminProfileData>(json) ?? new AdminProfileData();
            }
            var defaultProfile = new AdminProfileData();
            System.IO.File.WriteAllText(profileDbPath, JsonSerializer.Serialize(defaultProfile));
            return defaultProfile;
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var data = GetAdminProfileData();
            var model = new ProfileViewModel
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = data.Email,
                ProfilePictureUrl = data.ProfilePictureUrl
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model, IFormFile avatarFile, string currentPassword, string newPassword)
        {
            var data = GetAdminProfileData();

            if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
            {
                if (currentPassword == data.Password) data.Password = newPassword;
                else TempData["ToastMessage"] = "Mevcut şifreniz hatalı, şifre değiştirilemedi!";
            }

            if (avatarFile != null && avatarFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await avatarFile.CopyToAsync(ms);
                    data.ProfilePictureUrl = $"data:image/{Path.GetExtension(avatarFile.FileName).Replace(".", "")};base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }

            data.FirstName = model.FirstName;
            data.LastName = model.LastName;
            data.Email = model.Email;

            System.IO.File.WriteAllText(profileDbPath, JsonSerializer.Serialize(data));

            SystemLogger.AddLog("INFO", data.Email, "Yönetici profil bilgileri kalıcı olarak güncellendi.");
            TempData["ToastMessage"] = "Profil bilgileriniz başarıyla ve kalıcı olarak güncellendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Profile");
        }
    }

    public class AdminProfileData
    {
        public string FirstName { get; set; } = "Sistem";
        public string LastName { get; set; } = "Yöneticisi";
        public string Email { get; set; } = "patron@coredrive.com";
        public string Password { get; set; } = "aysegul123";
        public string ProfilePictureUrl { get; set; } = "https://ui-avatars.com/api/?name=Sistem+Yoneticisi&background=4e73df&color=fff&rounded=true";
    }
}