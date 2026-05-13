using DosyaYonetimPortali.MVC.Models;
using DosyaYonetimPortali.MVC.Data;
using DosyaYonetimPortali.MVC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Security.Claims;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        private static Dictionary<string, bool> _moduleDefinitions = new Dictionary<string, bool> {
            { "Dosya Yükleme ve İndirme", true },
            { "Klasör Oluşturma", true },
            { "Dosya Paylaşımı (Bağlantı ile)", false },
            { "Sistem Loglarını Görüntüleme", false },
            { "Kullanıcı Yönetimi", false },
            { "Kota Ayarları ve Yönetimi", false }
        };

        public AdminController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            ViewBag.PendingRequestGB = DriveController.PendingQuotaRequestGB;

            var activeItems = _context.DriveItems
                .Where(i => !i.IsDeleted)
                .ToList();

            int totalFiles = activeItems.Count(i => !i.IsFolder);
            int activeShares = activeItems.Count(i => i.IsShared);

            long usedBytes = activeItems
                .Where(i => !i.IsFolder)
                .Sum(i => i.SizeBytes);

            int userCount = _context.Users.Count();
            if (userCount <= 0)
                userCount = 1;

            long totalCapacityMB = userCount * DriveController.UserTotalQuotaMB;
            long totalBytes = totalCapacityMB * 1048576L;

            int storagePercent = totalBytes > 0
                ? (int)((usedBytes * 100) / totalBytes)
                : 0;

            ViewBag.TotalUsers = userCount;
            ViewBag.TotalFiles = totalFiles;
            ViewBag.ActiveShares = activeShares;
            ViewBag.StoragePercent = storagePercent > 100 ? 100 : storagePercent;

            var recentLogs = _context.SystemLogs
                .OrderByDescending(l => l.Id)
                .Take(6)
                .Select(l => new LogViewModel
                {
                    Status = l.Status,
                    UserEmail = l.UserEmail,
                    Message = l.Message,
                    Date = l.Date
                })
                .ToList();

            return View(recentLogs);
        }

        [HttpGet]
        public IActionResult Storage()
        {
            var activeFiles = _context.DriveItems
                .Where(i => !i.IsFolder && !i.IsDeleted)
                .ToList();

            int userCount = _context.Users.Count();
            if (userCount <= 0)
                userCount = 1;

            long totalCapacityMB = userCount * DriveController.UserTotalQuotaMB;
            long totalCapacityBytes = totalCapacityMB * 1048576L;

            long usedBytes = activeFiles.Sum(i => i.SizeBytes);

            long mediaBytes = activeFiles
                .Where(i => new[] { "jpg", "jpeg", "png", "gif", "mp4", "mov" }
                .Contains(i.Extension?.ToLower()))
                .Sum(i => i.SizeBytes);

            long docBytes = activeFiles
                .Where(i => new[] { "pdf", "doc", "docx", "xls", "xlsx", "txt", "csv" }
                .Contains(i.Extension?.ToLower()))
                .Sum(i => i.SizeBytes);

            long archiveBytes = activeFiles
                .Where(i => new[] { "zip", "rar", "7z", "tar" }
                .Contains(i.Extension?.ToLower()))
                .Sum(i => i.SizeBytes);

            ViewBag.UsedGB = (usedBytes / 1073741824.0).ToString("F2");
            ViewBag.TotalGB = (totalCapacityBytes / 1073741824.0).ToString("F2");

            ViewBag.StoragePercent = totalCapacityBytes > 0
                ? (int)((usedBytes * 100) / totalCapacityBytes)
                : 0;

            ViewBag.MediaGB = (mediaBytes / 1073741824.0).ToString("F2");
            ViewBag.DocGB = (docBytes / 1073741824.0).ToString("F2");
            ViewBag.ArchiveGB = (archiveBytes / 1073741824.0).ToString("F2");

            ViewBag.MediaPercent = usedBytes > 0 ? (int)((mediaBytes * 100) / usedBytes) : 0;
            ViewBag.DocPercent = usedBytes > 0 ? (int)((docBytes * 100) / usedBytes) : 0;
            ViewBag.ArchivePercent = usedBytes > 0 ? (int)((archiveBytes * 100) / usedBytes) : 0;

            return View();
        }

        [HttpPost]
        public IActionResult RefreshStorage()
        {
            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem Yöneticisi",
                Message = "Sunucu depolama verileri manuel olarak yenilendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["Message"] = "Depolama verileri başarıyla güncellendi.";

            return RedirectToAction("Storage");
        }

        [HttpGet]
        public IActionResult Logs()
        {
            var logs = _context.SystemLogs.OrderByDescending(l => l.Id)
                .Select(l => new LogViewModel
                {
                    Status = l.Status,
                    UserEmail = l.UserEmail,
                    Message = l.Message,
                    Date = l.Date
                }).ToList();

            return View(logs);
        }

        [HttpPost]
        public IActionResult ClearOldLogs()
        {
            var allLogs = _context.SystemLogs.ToList();

            _context.SystemLogs.RemoveRange(allLogs);

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "SYSTEM",
                UserEmail = "Sistem Yöneticisi",
                Message = "Tüm eski log kayıtları manuel olarak temizlendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Sistemdeki tüm eski log kayıtları başarıyla temizlendi.";
            TempData["ToastIcon"] = "success";

            return RedirectToAction("Logs");
        }

        [HttpGet]
        public IActionResult Shares()
        {
            var sharedFiles = _context.DriveItems
                .Where(i => i.IsShared && !i.IsDeleted)
                .Select(i => new DriveItemViewModel
                {
                    Id = i.Id,
                    Name = i.Name,
                    Owner = i.OwnerEmail,
                    ModifiedDate = i.ModifiedDate,
                    Extension = i.Extension
                }).ToList();

            return View(sharedFiles);
        }

        [HttpPost]
        public IActionResult RevokeShare(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);

            if (item != null)
            {
                item.IsShared = false;
                item.SharedWith = null;

                _context.SystemLogs.Add(new SystemLog
                {
                    Status = "INFO",
                    UserEmail = "Sistem Yöneticisi",
                    Message = $"'{item.Name}' dosyasının paylaşımı iptal edildi.",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });

                _context.SaveChanges();
            }

            TempData["Message"] = "Seçilen dosyanın paylaşım bağlantısı başarıyla iptal edildi.";

            return RedirectToAction("Shares");
        }

        [HttpPost]
        public IActionResult RevokeAllShares()
        {
            var items = _context.DriveItems.Where(i => i.IsShared).ToList();

            foreach (var item in items)
            {
                item.IsShared = false;
                item.SharedWith = null;
            }

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "WARN",
                UserEmail = "Sistem Yöneticisi",
                Message = "Sistemdeki tüm açık paylaşım bağlantıları kapatıldı.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["Message"] = "Sistemdeki tüm açık paylaşım bağlantıları başarıyla kapatıldı.";

            return RedirectToAction("Shares");
        }

        [HttpGet]
        public IActionResult Users()
        {
            var users = _context.Users.Select(u => new UserViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role
            }).ToList();

            return View(users);
        }

        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(UserViewModel model)
        {
            var newUser = new DosyaYonetimPortali.MVC.Models.Entities.User
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = "123",
                Role = model.Role,
                ProfilePictureUrl = $"https://ui-avatars.com/api/?name={model.FirstName}+{model.LastName}&background=random&color=fff&rounded=true"
            };

            _context.Users.Add(newUser);

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem Yöneticisi",
                Message = $"'{model.FirstName} {model.LastName}' isimli yeni kullanıcı sisteme eklendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Yeni kullanıcı başarıyla oluşturuldu.";
            TempData["ToastIcon"] = "success";

            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult DeleteUser(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                _context.Users.Remove(user);

                _context.SystemLogs.Add(new SystemLog
                {
                    Status = "WARN",
                    UserEmail = "Sistem Yöneticisi",
                    Message = $"'{user.FirstName} {user.LastName}' kullanıcısı sistemden silindi.",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });

                _context.SaveChanges();

                TempData["ToastMessage"] = "Kullanıcı sistemden silindi.";
                TempData["ToastIcon"] = "success";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult EditUser(string Id, string FirstName, string LastName, string Role)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == Id);

            if (user != null)
            {
                user.FirstName = FirstName;
                user.LastName = LastName;
                user.Role = Role;

                _context.SystemLogs.Add(new SystemLog
                {
                    Status = "INFO",
                    UserEmail = "Sistem Yöneticisi",
                    Message = $"'{FirstName} {LastName}' kullanıcısının bilgileri güncellendi.",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });

                _context.SaveChanges();

                TempData["ToastMessage"] = "Kullanıcı bilgileri güncellendi.";
                TempData["ToastIcon"] = "success";
            }

            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult LoginRecords()
        {
            var records = _context.LoginRecords
                .OrderByDescending(r => r.Id)
                .Select(r => new LoginRecordViewModel
                {
                    UserEmail = r.UserEmail,
                    IpAddress = r.IpAddress,
                    BrowserDevice = r.BrowserDevice,
                    Status = r.Status,
                    Date = r.Date
                }).ToList();

            return View(records);
        }

        [HttpPost]
        public IActionResult DownloadLoginRecordsCsv()
        {
            var builder = new StringBuilder();

            builder.AppendLine("Tarih/Saat,Kullanici,IP,Tarayici/Cihaz,Durum");

            var records = _context.LoginRecords.ToList();

            foreach (var record in records)
            {
                builder.AppendLine($"{record.Date},{record.UserEmail},{record.IpAddress},{record.BrowserDevice},{record.Status}");
            }

            return File(
                Encoding.UTF8.GetBytes(builder.ToString()),
                "text/csv",
                "GirisKayitlari.csv");
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

            return File(
                Encoding.UTF8.GetBytes(builder.ToString()),
                "text/csv",
                "DosyaHareketleri.csv");
        }

        [HttpGet]
        public IActionResult QuotaManagement()
        {
            ViewBag.CurrentQuotaMB = DriveController.UserTotalQuotaMB;
            ViewBag.PendingRequestGB = DriveController.PendingQuotaRequestGB;

            return View();
        }

        [HttpPost]
        public IActionResult ApproveQuotaRequest()
        {
            if (DriveController.PendingQuotaRequestGB > 0)
            {
                DriveController.UserTotalQuotaMB =
                    DriveController.PendingQuotaRequestGB * 1024;

                DriveController.PendingQuotaRequestGB = 0;

                _context.SystemLogs.Add(new SystemLog
                {
                    Status = "INFO",
                    UserEmail = "Sistem",
                    Message = "Kullanıcının kota artırım talebi onaylandı.",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });

                _context.SaveChanges();

                TempData["ToastMessage"] = "Kota talebi başarıyla onaylandı.";
                TempData["ToastIcon"] = "success";
            }

            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult RejectQuotaRequest()
        {
            DriveController.PendingQuotaRequestGB = 0;

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "WARN",
                UserEmail = "Sistem",
                Message = "Kullanıcının kota artırım talebi reddedildi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Kota talebi reddedildi.";
            TempData["ToastIcon"] = "error";

            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult ForceSetQuota(int targetGB)
        {
            DriveController.UserTotalQuotaMB = (long)targetGB * 1024;

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "ADMIN",
                UserEmail = "Sistem",
                Message = $"Kullanıcı kotası {targetGB} GB olarak değiştirildi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Kota başarıyla güncellendi.";
            TempData["ToastIcon"] = "success";

            return RedirectToAction("QuotaManagement");
        }

        [HttpPost]
        public IActionResult GenerateSystemReport()
        {
            int totalUsers = _context.Users.Count();

            int totalFiles = _context.DriveItems
                .Count(i => !i.IsFolder && !i.IsDeleted);

            int activeShares = _context.DriveItems
                .Count(i => i.IsShared && !i.IsDeleted);

            long usedBytes = _context.DriveItems
                .Where(i => !i.IsFolder && !i.IsDeleted)
                .Sum(i => i.SizeBytes);

            double usedGB = usedBytes / 1073741824.0;

            int userCount = totalUsers <= 0 ? 1 : totalUsers;

            long totalCapacityMB =
                userCount * DriveController.UserTotalQuotaMB;

            long totalBytes = totalCapacityMB * 1048576L;

            int storagePercent = totalBytes > 0
                ? (int)((usedBytes * 100) / totalBytes)
                : 0;

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem Yöneticisi",
                Message = "Genel sistem raporu oluşturuldu.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                document.Add(
                    new Paragraph("CORE-DRIVE SISTEM RAPORU")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(22));

                document.Add(
                    new Paragraph("Rapor Tarihi: " +
                    DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(10));

                document.Add(new Paragraph("-----------------------------------------"));

                document.Add(new Paragraph($"Toplam Kullanici: {totalUsers}"));
                document.Add(new Paragraph($"Toplam Dosya: {totalFiles}"));
                document.Add(new Paragraph($"Aktif Paylasim: {activeShares}"));
                document.Add(new Paragraph($"Kullanilan Alan: {usedGB:F2} GB"));
                document.Add(new Paragraph($"Doluluk Orani: %{storagePercent}"));

                document.Add(new Paragraph("-----------------------------------------"));

                document.Add(
                    new Paragraph("Sistem Durumu: AKTIF")
                    .SetFontSize(14));

                document.Close();

                return File(
                    ms.ToArray(),
                    "application/pdf",
                    $"CoreDrive_Rapor_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [HttpGet]
        public IActionResult Profile(User? user1)
        {
            string? currentEmail = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _context.Users
                .FirstOrDefault(u => u.Email == currentEmail);

            var model = new ProfileViewModel
            {
                FirstName = user?.FirstName ?? "Sistem",
                LastName = user?.LastName ?? "Yöneticisi",
                Email = user1?.Email ?? currentEmail,
                ProfilePictureUrl = user?.ProfilePictureUrl ??
                "https://ui-avatars.com/api/?name=Admin&background=4e73df&color=fff&rounded=true"
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(
            ProfileViewModel model,
            IFormFile avatarFile,
            string currentPassword,
            string newPassword)
        {
            string? currentEmail = User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = _context.Users
                .FirstOrDefault(u => u.Email == currentEmail);

            if (user != null)
            {
                if (!string.IsNullOrEmpty(currentPassword)
                    && !string.IsNullOrEmpty(newPassword))
                {
                    if (currentPassword == user.Password)
                    {
                        user.Password = newPassword;
                    }
                    else
                    {
                        TempData["ToastMessage"] =
                            "Mevcut şifre yanlış!";
                    }
                }

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await avatarFile.CopyToAsync(ms);

                        user.ProfilePictureUrl =
                            $"data:image/{Path.GetExtension(avatarFile.FileName).Replace(".", "")};base64,{Convert.ToBase64String(ms.ToArray())}";
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;

                _context.SystemLogs.Add(new SystemLog
                {
                    Status = "INFO",
                    UserEmail = user.Email,
                    Message = "Yönetici profil bilgileri güncellendi.",
                    Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
                });

                _context.SaveChanges();

                TempData["ToastMessage"] =
                    "Profil başarıyla güncellendi.";

                TempData["ToastIcon"] = "success";
            }

            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Roles()
        {
            var existingRoles = _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Role))
                .Select(u => u.Role)
                .Distinct()
                .ToList();

            var coreRoles = new List<string> { "Admin", "Standart Kullanıcı" };

            var allRoles = coreRoles.Union(existingRoles).ToList();

            var model = new List<RoleViewModel>();

            foreach (var role in allRoles)
            {
                model.Add(new RoleViewModel
                {
                    RoleName = role,
                    UserCount = _context.Users.Count(u => u.Role == role),
                    IsSystemRole = (role == "Admin" || role == "Standart Kullanıcı")
                });
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult AddRole(string RoleName)
        {
            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem",
                Message = $"'{RoleName}' isimli yeni rol sisteme eklendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            _context.SaveChanges();

            TempData["ToastMessage"] = "Yeni rol başarıyla oluşturuldu.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public IActionResult EditRole(string OldRoleName, string NewRoleName)
        {
            var users = _context.Users.Where(u => u.Role == OldRoleName).ToList();
            foreach (var u in users)
            {
                u.Role = NewRoleName;
            }

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem",
                Message = $"'{OldRoleName}' rolü '{NewRoleName}' olarak güncellendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Rol başarıyla güncellendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Roles");
        }

        [HttpPost]
        public IActionResult DeleteRole(string RoleName)
        {
            var users = _context.Users.Where(u => u.Role == RoleName).ToList();
            foreach (var u in users)
            {
                u.Role = "Standart Kullanıcı";
            }

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "WARN",
                UserEmail = "Sistem",
                Message = $"'{RoleName}' rolü silindi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });

            _context.SaveChanges();

            TempData["ToastMessage"] = "Rol silindi ve kullanıcılar güncellendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Roles");
        }

        [HttpGet]
        public IActionResult Permissions()
        {
            var existingRoles = _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Role))
                .Select(u => u.Role)
                .Distinct()
                .ToList();

            if (!existingRoles.Contains("Admin")) existingRoles.Add("Admin");
            if (!existingRoles.Contains("Standart Kullanıcı")) existingRoles.Add("Standart Kullanıcı");

            ViewBag.Roles = existingRoles.Select(r => new RoleViewModel { RoleName = r }).ToList();

            var modules = new List<PermissionViewModel>();

            foreach (var mod in _moduleDefinitions)
            {
                var pvm = new PermissionViewModel
                {
                    ModuleName = mod.Key,
                    IsCore = mod.Value,
                    RoleAccesses = new List<RoleAccessViewModel>()
                };

                foreach (var role in existingRoles)
                {
                    pvm.RoleAccesses.Add(new RoleAccessViewModel
                    {
                        RoleName = role,
                        HasAccess = (role == "Admin")
                    });
                }
                modules.Add(pvm);
            }

            return View(modules);
        }

        [HttpPost]
        public IActionResult AddPermission(string ModuleName, string ModuleType)
        {
            bool isCore = ModuleType == "Core";

            if (!_moduleDefinitions.ContainsKey(ModuleName))
            {
                _moduleDefinitions.Add(ModuleName, isCore);
            }

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem Yöneticisi",
                Message = $"'{ModuleName}' isimli yeni {(isCore ? "çekirdek" : "serbest")} yetki modülü eklendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            _context.SaveChanges();

            TempData["ToastMessage"] = "Yeni yetki modülü başarıyla eklendi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Permissions");
        }

        [HttpPost]
        public IActionResult DeletePermission(string ModuleName)
        {
            if (_moduleDefinitions.ContainsKey(ModuleName))
            {
                _moduleDefinitions.Remove(ModuleName);
            }

            _context.SystemLogs.Add(new SystemLog
            {
                Status = "WARN",
                UserEmail = "Sistem Yöneticisi",
                Message = $"'{ModuleName}' yetki modülü sistemden silindi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            _context.SaveChanges();

            TempData["ToastMessage"] = "Yetki modülü başarıyla silindi.";
            TempData["ToastIcon"] = "success";
            return RedirectToAction("Permissions");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SaveSettings()
        {
            _context.SystemLogs.Add(new SystemLog
            {
                Status = "INFO",
                UserEmail = "Sistem Yöneticisi",
                Message = "Genel sistem ayarları güncellendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            _context.SaveChanges();

            TempData["ToastMessage"] = "Sistem ayarları başarıyla kaydedildi.";
            TempData["ToastIcon"] = "success";

            return RedirectToAction("Settings");
        }

        [HttpGet]
        public IActionResult FileSettings()
        {
            return View(new FileSettingsViewModel());
        }

        [HttpPost]
        public IActionResult SaveFileSettings()
        {
            _context.SystemLogs.Add(new SystemLog
            {
                Status = "WARN",
                UserEmail = "Sistem Yöneticisi",
                Message = "Dosya yükleme kısıtlamaları ve limit ayarları güncellendi.",
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            });
            _context.SaveChanges();

            TempData["ToastMessage"] = "Dosya ve güvenlik ayarları başarıyla kaydedildi.";
            TempData["ToastIcon"] = "success";

            return RedirectToAction("FileSettings");
        }
    }
}