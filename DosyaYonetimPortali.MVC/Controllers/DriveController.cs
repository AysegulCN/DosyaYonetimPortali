using DosyaYonetimPortali.MVC.Models;
using DosyaYonetimPortali.MVC.Data;
using DosyaYonetimPortali.MVC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        private readonly AppDbContext _context;
        public static long UserTotalQuotaMB = 15360;
        public static int PendingQuotaRequestGB = 0;

        public static string CurrentFullName = "Kullanıcı";
        public static string CurrentEmail = "kullanici@coredrive.com";
        public static string CurrentPhone = "";
        public static string CurrentAvatarBase64 = "";

        public DriveController(AppDbContext context)
        {
            _context = context;
        }

        public static (string Used, string Total, int Percent) GetStorageInfo(AppDbContext context)
        {
            long usedBytes = context.DriveItems.Where(i => !i.IsFolder && !i.IsDeleted).Sum(i => i.SizeBytes);
            double usedMB = usedBytes / 1048576.0;
            int percent = (int)((usedMB / UserTotalQuotaMB) * 100);
            string usedStr = usedMB >= 1024 ? $"{(usedMB / 1024):F2} GB" : $"{usedMB:F1} MB";
            string totalStr = UserTotalQuotaMB >= 1024 ? $"{(UserTotalQuotaMB / 1024)} GB" : $"{UserTotalQuotaMB} MB";
            return (usedStr, totalStr, percent > 100 ? 100 : percent);
        }

        [HttpPost]
        public IActionResult UpdateSettings(bool emailNotif, bool sysNotif)
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentEmail);
            if (user != null)
            {
                user.EmailNotifications = emailNotif;
                user.SystemNotifications = sysNotif;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Bildirim ayarlarınız başarıyla güncellendi.";
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phone, string currentPassword, string newPassword, IFormFile avatarFile, string returnUrl)
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == currentEmail);

            if (user != null)
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    TempData["StorageError"] = "Güvenliğiniz için profil güncellemelerinde mevcut şifrenizi girmelisiniz.";
                }
                else if (user.Password != currentPassword)
                {
                    TempData["StorageError"] = "Mevcut şifrenizi yanlış girdiniz.";
                }
                else
                {
                    user.FirstName = fullName?.Split(' ').First() ?? user.FirstName;
                    user.LastName = fullName?.Contains(" ") == true ? fullName.Substring(fullName.IndexOf(" ") + 1) : user.LastName;
                    CurrentFullName = fullName;
                    CurrentPhone = phone;

                    if (!string.IsNullOrEmpty(newPassword)) user.Password = newPassword;

                    if (avatarFile != null && avatarFile.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            avatarFile.CopyTo(ms);
                            string base64 = $"data:{avatarFile.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
                            user.ProfilePictureUrl = base64;
                            CurrentAvatarBase64 = base64;
                        }
                    }
                    _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = user.Email, Message = "Profil bilgilerini güncelledi.", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                }
            }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost]
        public IActionResult RequestQuotaUpgrade(int requestedQuotaGB, string returnUrl)
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? CurrentFullName;
            PendingQuotaRequestGB = requestedQuotaGB;
            _context.SystemLogs.Add(new SystemLog { Status = "Kota Aşımı", UserEmail = currentEmail, Message = $"Sistemden {requestedQuotaGB} GB kota artırım talebinde bulundu.", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"{requestedQuotaGB} GB depolama alanı talebiniz sistem yöneticisine iletildi.";
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpGet]
        public IActionResult Dashboard(string folderId = null, string searchQuery = null)
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            string activeFolderId = string.IsNullOrEmpty(folderId) ? "" : folderId;

            ViewBag.CurrentFolderId = activeFolderId;
            ViewBag.SearchQuery = searchQuery;

            var breadcrumb = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Name = "Drive'ım", FolderId = "" }
            };

            if (!string.IsNullOrEmpty(activeFolderId) && string.IsNullOrEmpty(searchQuery))
            {
                var currentFolder = _context.DriveItems.FirstOrDefault(f => f.Id == activeFolderId);
                if (currentFolder != null)
                {
                    ViewBag.CurrentFolderName = currentFolder.Name;
                    var parents = new List<BreadcrumbItem>();
                    var tempFolder = currentFolder;
                    while (tempFolder != null)
                    {
                        parents.Insert(0, new BreadcrumbItem { Name = tempFolder.Name, FolderId = tempFolder.Id });
                        tempFolder = _context.DriveItems.FirstOrDefault(f => f.Id == tempFolder.ParentId);
                    }
                    breadcrumb.AddRange(parents);
                }
            }
            else if (!string.IsNullOrEmpty(searchQuery))
            {
                ViewBag.CurrentFolderName = $"Arama Sonuçları: '{searchQuery}'";
            }

            ViewBag.Breadcrumb = breadcrumb;
            ViewBag.SystemUsers = _context.Users.Where(u => u.Email != currentEmail).Select(u => new UserViewModel { FirstName = u.FirstName, LastName = u.LastName, Email = u.Email }).ToList();

            var query = _context.DriveItems.Where(i => !i.IsDeleted && (i.OwnerEmail == currentEmail || i.SharedWith == currentEmail));

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(i => i.Name.Contains(searchQuery));
            }
            else
            {
                query = query.Where(i => i.ParentId == activeFolderId);
            }

            var items = query.ToList();

            var viewModelItems = items.Select(i => new DriveItemViewModel
            {
                Id = i.Id,
                ParentId = i.ParentId,
                Name = i.Name,
                IsFolder = i.IsFolder,
                Extension = i.Extension,
                Size = i.Size,
                SizeBytes = i.SizeBytes,
                ModifiedDate = i.ModifiedDate,
                Owner = i.OwnerEmail,
                IsShared = i.IsShared,
                SharedWith = i.SharedWith,
                IsDeleted = i.IsDeleted,
                ContentType = i.ContentType,
                FileData = i.FileData
            }).ToList();

            return View(viewModelItems);
        }

        [HttpPost]
        public IActionResult CreateFolder(string folderName, string parentId)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                string safeParentId = string.IsNullOrEmpty(parentId) ? "" : parentId;

                var folder = new DriveItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentId = safeParentId,
                    Name = folderName,
                    IsFolder = true,
                    ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    OwnerEmail = currentEmail,
                    SizeBytes = 0,
                    ContentType = "folder",
                    Extension = "",
                    Size = "0 KB",
                    FileData = "",
                    SharedWith = "",
                    IsShared = false,
                    IsDeleted = false
                };

                _context.DriveItems.Add(folder);
                _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = currentEmail, Message = $"Yeni Klasör Oluşturuldu: {folderName}", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                _context.SaveChanges();
            }
            return RedirectToAction("Dashboard", new { folderId = parentId });
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string parentId)
        {
            if (file != null && file.Length > 0)
            {
                string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                long usedBytes = _context.DriveItems.Where(i => i.OwnerEmail == currentEmail || i.SharedWith == currentEmail).Sum(i => i.SizeBytes);

                if ((usedBytes + file.Length) / 1048576.0 > UserTotalQuotaMB)
                {
                    TempData["StorageError"] = "Yetersiz depolama alanı!";
                    return RedirectToAction("Dashboard", new { folderId = parentId });
                }

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                string safeParentId = string.IsNullOrEmpty(parentId) ? "" : parentId;

                var newFile = new DriveItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentId = safeParentId,
                    Name = file.FileName,
                    IsFolder = false,
                    Extension = Path.GetExtension(file.FileName).Replace(".", "").ToLower(),
                    Size = (file.Length / 1024) > 1024 ? $"{(file.Length / 1048576f):F1} MB" : $"{(file.Length / 1024)} KB",
                    SizeBytes = file.Length,
                    ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    OwnerEmail = currentEmail,
                    ContentType = file.ContentType,
                    FileData = uniqueFileName,
                    SharedWith = "",
                    IsShared = false,
                    IsDeleted = false
                };

                _context.DriveItems.Add(newFile);
                _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = currentEmail, Message = $"Yeni Dosya Yüklendi: {file.FileName}", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                _context.SaveChanges();
            }
            return RedirectToAction("Dashboard", new { folderId = parentId });
        }

        [HttpGet]
        public IActionResult DownloadFile(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item == null || string.IsNullOrEmpty(item.FileData)) return NotFound();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", item.FileData);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, item.ContentType, item.Name);
        }

        [HttpGet]
        public IActionResult PreviewFile(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item == null || string.IsNullOrEmpty(item.FileData)) return NotFound();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", item.FileData);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

            if (item.ContentType != null && item.ContentType.StartsWith("image/"))
            {
                string base64 = Convert.ToBase64String(fileBytes);
                string imgHtml = $@"<!DOCTYPE html><html><body style='background:#f1f3f4; display:flex; align-items:center; justify-content:center; height:100vh; margin:0;'><img src='data:{item.ContentType};base64,{base64}' style='max-width:95%; max-height:95%; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'></body></html>";
                return Content(imgHtml, "text/html", System.Text.Encoding.UTF8);
            }

            var supportedTypes = new[] { "application/pdf", "video/mp4", "text/plain" };
            if (item.ContentType != null && supportedTypes.Contains(item.ContentType))
            {
                return File(fileBytes, item.ContentType);
            }

            string fallbackHtml = $@"
            <!DOCTYPE html>
            <html lang='tr'>
            <head>
                <meta charset='utf-8'>
                <title>{item.Name} - Önizleme</title>
                <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css' rel='stylesheet'>
                <style>
                    body {{ background-color: #f8f9fa; font-family: 'Segoe UI', sans-serif; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }}
                    .card {{ background: white; padding: 40px; border-radius: 16px; box-shadow: 0 4px 24px rgba(0,0,0,0.06); text-align: center; max-width: 420px; }}
                    .icon {{ font-size: 72px; color: #1a73e8; margin-bottom: 24px; }}
                    .btn {{ display: inline-block; background: #1a73e8; color: white; text-decoration: none; padding: 12px 28px; border-radius: 24px; font-weight: bold; margin-top: 24px; transition: 0.2s; font-size: 15px; }}
                    .btn:hover {{ background: #1557b0; box-shadow: 0 2px 6px rgba(26,115,232,0.4); }}
                </style>
            </head>
            <body>
                <div class='card'>
                    <i class='fas fa-file-alt icon'></i>
                    <h4 style='margin:0 0 12px 0; color:#202124; font-size: 18px; word-wrap: break-word;'>{item.Name}</h4>
                    <p style='color:#5f6368; font-size:14px; margin:0; line-height: 1.5;'>Bu dosya türü (<b>{item.Extension.ToUpper()}</b>) için tarayıcı önizlemesi desteklenmiyor.</p>
                    <a href='/Drive/DownloadFile?id={item.Id}' class='btn'><i class='fas fa-download' style='margin-right: 8px;'></i> Dosyayı İndir</a>
                </div>
            </body>
            </html>";

            return Content(fallbackHtml, "text/html", System.Text.Encoding.UTF8);
        }

        [HttpPost]
        public IActionResult ShareItem(string id, string email, string returnUrl)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item != null && !string.IsNullOrEmpty(email))
            {
                item.IsShared = true;
                item.SharedWith = email;
                string currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = currentUserEmail, Message = $"'{item.Name}' dosyası {email} kullanıcısına gönderildi.", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Dosya başarıyla {email} kullanıcısına paylaşıldı!";
            }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost]
        public IActionResult MoveToTrash(string id, string returnUrl)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsDeleted = true; _context.SaveChanges(); }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost]
        public IActionResult RestoreFromTrash(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsDeleted = false; _context.SaveChanges(); }
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult DeletePermanently(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                if (!item.IsFolder && !string.IsNullOrEmpty(item.FileData))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", item.FileData);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }
                _context.DriveItems.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult EmptyTrash()
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var trashedItems = _context.DriveItems.Where(i => i.IsDeleted && i.OwnerEmail == currentEmail).ToList();

            foreach (var item in trashedItems)
            {
                if (!item.IsFolder && !string.IsNullOrEmpty(item.FileData))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", item.FileData);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }
            }

            _context.DriveItems.RemoveRange(trashedItems);
            _context.SaveChanges();
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult UnshareItem(string id)
        {
            var item = _context.DriveItems.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsShared = false; item.SharedWith = ""; _context.SaveChanges(); }
            return RedirectToAction("Shared");
        }

        [HttpGet]
        public IActionResult Shared()
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var sharedWithMe = _context.DriveItems.Where(i => i.IsShared && !i.IsDeleted && i.SharedWith == currentEmail).ToList();

            var viewModelItems = sharedWithMe.Select(i => new DriveItemViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Owner = i.OwnerEmail,
                ModifiedDate = i.ModifiedDate,
                Size = i.Size,
                Extension = i.Extension
            }).ToList();

            return View(viewModelItems);
        }

        [HttpGet]
        public IActionResult Trash()
        {
            string currentEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var deletedItems = _context.DriveItems.Where(i => i.IsDeleted && i.OwnerEmail == currentEmail).ToList();

            var viewModelItems = deletedItems.Select(i => new DriveItemViewModel
            {
                Id = i.Id,
                Name = i.Name,
                Owner = i.OwnerEmail,
                ModifiedDate = i.ModifiedDate,
                Size = i.Size,
                Extension = i.Extension
            }).ToList();

            return View(viewModelItems);
        }
    }

    public class BreadcrumbItem
    {
        public string Name { get; set; }
        public string FolderId { get; set; }
    }
}