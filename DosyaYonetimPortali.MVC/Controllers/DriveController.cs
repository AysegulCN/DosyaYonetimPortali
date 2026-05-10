using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.IO;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        private readonly string _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");

        public static long UserTotalQuotaMB = 15360;
        public static int PendingQuotaRequestGB = 0;

        public static string CurrentFullName = "Kullanıcı";
        public static string CurrentEmail = "kullanici@coredrive.com";
        public static string CurrentPhone = "";
        public static string CurrentAvatarBase64 = "";

        [HttpPost]
        public IActionResult UpdateProfile(string fullName, string email, string phone, string currentPassword, string newPassword, IFormFile avatarFile, string returnUrl)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                TempData["StorageError"] = "Güvenliğiniz için profil güncellemelerinde mevcut şifrenizi girmelisiniz.";
            }
            else
            {
                CurrentFullName = fullName ?? "Kullanıcı";
                CurrentEmail = email ?? "kullanici@coredrive.com";
                CurrentPhone = phone;

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        avatarFile.CopyTo(ms);
                        CurrentAvatarBase64 = $"data:{avatarFile.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
                    }
                }
                TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
            }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        public static (string Used, string Total, int Percent) GetStorageInfo()
        {
            long usedBytes = 0;
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");
            if (System.IO.File.Exists(dbPath))
            {
                var json = System.IO.File.ReadAllText(dbPath);
                var db = JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
                usedBytes = db.Where(i => i.Owner != "patron@coredrive.com" && i.Owner != "muhasebe@coredrive.com").Sum(i => i.SizeBytes);
            }

            double usedMB = usedBytes / 1048576.0;
            int percent = (int)((usedMB / UserTotalQuotaMB) * 100);
            string usedStr = usedMB >= 1024 ? $"{(usedMB / 1024):F2} GB" : $"{usedMB:F1} MB";
            string totalStr = UserTotalQuotaMB >= 1024 ? $"{(UserTotalQuotaMB / 1024)} GB" : $"{UserTotalQuotaMB} MB";

            return (usedStr, totalStr, percent > 100 ? 100 : percent);
        }

        [HttpPost]
        public IActionResult RequestQuotaUpgrade(int requestedQuotaGB, string returnUrl)
        {
            PendingQuotaRequestGB = requestedQuotaGB;
            TempData["SuccessMessage"] = $"{requestedQuotaGB} GB depolama alanı talebiniz sistem yöneticisine iletildi. Onaylandığında kotanız güncellenecektir.";
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        private List<DriveItemViewModel> GetDatabase()
        {
            if (System.IO.File.Exists(_dbPath))
            {
                var json = System.IO.File.ReadAllText(_dbPath);
                return JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
            }

            var initialData = new List<DriveItemViewModel>
            {
                new DriveItemViewModel { Id = "s2", Name = "Proje_Butcesi.xlsx", IsFolder = false, Extension = "xlsx", Size = "1.1 MB", SizeBytes = 1153433, ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = "Ben", IsShared = true, SharedWith = "muhasebe@coredrive.com" },
                new DriveItemViewModel { Id = "t1", Name = "Eski_Tasarimlar.zip", IsFolder = false, Extension = "zip", Size = "145 MB", SizeBytes = 152043520, ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = "Ben", IsDeleted = true },
                new DriveItemViewModel { Id = "s1", Name = "Yillik_Rapor_2025.pdf", IsFolder = false, Extension = "pdf", Size = "4.2 MB", SizeBytes = 4404019, ModifiedDate = "08.05.2026 14:30", Owner = "patron@coredrive.com", IsShared = true }
            };
            SaveDatabase(initialData);
            return initialData;
        }

        private void SaveDatabase(List<DriveItemViewModel> data)
        {
            var json = JsonSerializer.Serialize(data);
            System.IO.File.WriteAllText(_dbPath, json);
        }

        [HttpGet]
        public IActionResult Dashboard(string folderId = null)
        {
            var db = GetDatabase();
            ViewBag.CurrentFolderId = folderId;
            if (!string.IsNullOrEmpty(folderId)) { ViewBag.CurrentFolderName = db.FirstOrDefault(f => f.Id == folderId)?.Name; }
            string currentUser = User.Identity.Name ?? "Kullanıcı";
            return View(db.Where(i => i.ParentId == folderId && !i.IsDeleted && (i.Owner == "Ben" || i.Owner == currentUser)).ToList());
        }

        [HttpPost]
        public IActionResult CreateFolder(string folderName, string parentId)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                var db = GetDatabase();
                db.Insert(0, new DriveItemViewModel { Id = Guid.NewGuid().ToString(), ParentId = parentId, Name = folderName, IsFolder = true, ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = User.Identity.Name ?? "Kullanıcı", SizeBytes = 0 });
                SaveDatabase(db);
            }
            return RedirectToAction("Dashboard", new { folderId = parentId });
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string parentId)
        {
            if (file != null && file.Length > 0)
            {
                var db = GetDatabase();
                string currentUser = User.Identity.Name ?? "Kullanıcı";
                long usedBytes = db.Where(i => i.Owner == "Ben" || i.Owner == currentUser).Sum(i => i.SizeBytes);

                double totalRequestedMB = (usedBytes + file.Length) / 1048576.0;
                if (totalRequestedMB > UserTotalQuotaMB)
                {
                    TempData["StorageError"] = "Yetersiz depolama alanı! Lütfen dosya silerek yer açın veya kotanızı yükseltin.";
                    return RedirectToAction("Dashboard", new { folderId = parentId });
                }

                string base64Data = null;
                using (var ms = new MemoryStream()) { file.CopyTo(ms); base64Data = Convert.ToBase64String(ms.ToArray()); }

                db.Add(new DriveItemViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentId = parentId,
                    Name = file.FileName,
                    IsFolder = false,
                    Extension = Path.GetExtension(file.FileName).Replace(".", "").ToLower(),
                    Size = (file.Length / 1024) > 1024 ? $"{(file.Length / 1048576f):F1} MB" : $"{(file.Length / 1024)} KB",
                    SizeBytes = file.Length,
                    ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    Owner = currentUser,
                    FileData = base64Data,
                    ContentType = file.ContentType
                });
                SaveDatabase(db);
            }
            return RedirectToAction("Dashboard", new { folderId = parentId });
        }

        [HttpGet]
        public IActionResult PreviewFile(string id)
        {
            var item = GetDatabase().FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();

            if (!string.IsNullOrEmpty(item.FileData) && item.ContentType != null && item.ContentType.StartsWith("image/"))
            {
                string imgHtml = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{item.Name}</title></head><body style='background:#0f0f0f; display:flex; align-items:center; justify-content:center; height:100vh; margin:0;'><img src='data:{item.ContentType};base64,{item.FileData}' style='max-width:95%; max-height:95%; box-shadow:0 10px 30px rgba(0,0,0,0.8); border-radius: 4px;'></body></html>";
                return Content(imgHtml, "text/html", Encoding.UTF8);
            }
            if (!string.IsNullOrEmpty(item.FileData) && item.ContentType == "application/pdf") return File(Convert.FromBase64String(item.FileData), item.ContentType);
            if (!string.IsNullOrEmpty(item.FileData)) return File(Convert.FromBase64String(item.FileData), item.ContentType, item.Name);

            string html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{item.Name}</title><link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css' rel='stylesheet'></head><body style='background:#202124; color:#fff; display:flex; flex-direction:column; align-items:center; justify-content:center; height:100vh; margin:0; font-family:""Segoe UI"",sans-serif;'><div style='background:#303134; padding:40px 60px; border-radius:12px; text-align:center;'><i class='fas fa-file' style='font-size:64px; color:#8ab4f8; margin-bottom:20px;'></i><h2 style='margin:0 0 10px 0; font-weight:500;'>{item.Name}</h2><p style='color:#9aa0a6;'>Simülasyon Dosyası</p></div></body></html>";
            return Content(html, "text/html", Encoding.UTF8);
        }

        [HttpGet]
        public IActionResult DownloadFile(string id)
        {
            var item = GetDatabase().FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();
            if (!string.IsNullOrEmpty(item.FileData)) return File(Convert.FromBase64String(item.FileData), item.ContentType, item.Name);

            string contentType = item.Extension == "pdf" ? "application/pdf" : item.Extension == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : item.Extension == "zip" ? "application/zip" : "application/octet-stream";
            return File(Encoding.UTF8.GetBytes("Simülasyon dosyası."), contentType, item.Name);
        }

        [HttpPost]
        public IActionResult ShareItem(string id, string email, string returnUrl)
        {
            var db = GetDatabase(); var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null && !string.IsNullOrEmpty(email)) { item.IsShared = true; item.SharedWith = email; SaveDatabase(db); }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost] public IActionResult MoveToTrash(string id, string returnUrl) { var db = GetDatabase(); var item = db.FirstOrDefault(i => i.Id == id); if (item != null) { item.IsDeleted = true; SaveDatabase(db); } return Redirect(returnUrl ?? "/Drive/Dashboard"); }
        [HttpPost] public IActionResult RestoreFromTrash(string id) { var db = GetDatabase(); var item = db.FirstOrDefault(i => i.Id == id); if (item != null) { item.IsDeleted = false; SaveDatabase(db); } return RedirectToAction("Trash"); }
        [HttpPost] public IActionResult DeletePermanently(string id) { var db = GetDatabase(); var item = db.FirstOrDefault(i => i.Id == id); if (item != null) { db.Remove(item); SaveDatabase(db); } return RedirectToAction("Trash"); }
        [HttpPost] public IActionResult EmptyTrash() { var db = GetDatabase(); db.RemoveAll(i => i.IsDeleted); SaveDatabase(db); return RedirectToAction("Trash"); }
        [HttpPost] public IActionResult UnshareItem(string id) { var db = GetDatabase(); var item = db.FirstOrDefault(i => i.Id == id); if (item != null) { item.IsShared = false; SaveDatabase(db); } return RedirectToAction("Shared"); }

        [HttpGet] public IActionResult Shared() { return View(GetDatabase().Where(i => i.IsShared && !i.IsDeleted).ToList()); }
        [HttpGet] public IActionResult Trash() { return View(GetDatabase().Where(i => i.IsDeleted).ToList()); }
    }
}