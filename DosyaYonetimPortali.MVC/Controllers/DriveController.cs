using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.IO;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        private readonly string _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "coredrive_db.json");

        private List<DriveItemViewModel> GetDatabase()
        {
            if (System.IO.File.Exists(_dbPath))
            {
                var json = System.IO.File.ReadAllText(_dbPath);
                return JsonSerializer.Deserialize<List<DriveItemViewModel>>(json) ?? new List<DriveItemViewModel>();
            }

            var initialData = new List<DriveItemViewModel>
            {
                new DriveItemViewModel { Id = "s2", Name = "Proje_Butcesi.xlsx", IsFolder = false, Extension = "xlsx", Size = "1.1 MB", ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = "Ben", IsShared = true, SharedWith = "muhasebe@coredrive.com" },
                new DriveItemViewModel { Id = "t1", Name = "Eski_Tasarimlar.zip", IsFolder = false, Extension = "zip", Size = "145 MB", ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = "Ben", IsDeleted = true },
                new DriveItemViewModel { Id = "s1", Name = "Yillik_Rapor_2025.pdf", IsFolder = false, Extension = "pdf", Size = "4.2 MB", ModifiedDate = "08.05.2026 14:30", Owner = "patron@coredrive.com", IsShared = true }
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

            if (!string.IsNullOrEmpty(folderId))
            {
                var currentFolder = db.FirstOrDefault(f => f.Id == folderId);
                ViewBag.CurrentFolderName = currentFolder?.Name;
            }

            var itemsToShow = db.Where(i => i.ParentId == folderId && !i.IsDeleted && !i.IsShared).ToList();
            return View(itemsToShow);
        }

        [HttpPost]
        public IActionResult CreateFolder(string folderName, string parentId)
        {
            if (!string.IsNullOrEmpty(folderName))
            {
                var db = GetDatabase();
                db.Insert(0, new DriveItemViewModel { Id = Guid.NewGuid().ToString(), ParentId = parentId, Name = folderName, IsFolder = true, ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Owner = User.Identity.Name ?? "Kullanıcı" });
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
                string base64Data = null;
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    base64Data = Convert.ToBase64String(ms.ToArray());
                }

                db.Add(new DriveItemViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    ParentId = parentId,
                    Name = file.FileName,
                    IsFolder = false,
                    Extension = Path.GetExtension(file.FileName).Replace(".", "").ToLower(),
                    Size = (file.Length / 1024) > 1024 ? $"{(file.Length / 1048576f):F1} MB" : $"{(file.Length / 1024)} KB",
                    ModifiedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    Owner = User.Identity.Name ?? "Kullanıcı",
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
                string imgSrc = $"data:{item.ContentType};base64,{item.FileData}";
                string imgHtml = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{item.Name}</title></head>
                <body style='background:#0f0f0f; display:flex; align-items:center; justify-content:center; height:100vh; margin:0;'>
                    <img src='{imgSrc}' style='max-width:95%; max-height:95%; box-shadow:0 10px 30px rgba(0,0,0,0.8); border-radius: 4px;'>
                </body></html>";
                return Content(imgHtml, "text/html", System.Text.Encoding.UTF8);
            }

            if (!string.IsNullOrEmpty(item.FileData) && item.ContentType == "application/pdf")
            {
                byte[] bytes = Convert.FromBase64String(item.FileData);
                return File(bytes, item.ContentType);
            }

            if (!string.IsNullOrEmpty(item.FileData))
            {
                byte[] bytes = Convert.FromBase64String(item.FileData);
                return File(bytes, item.ContentType, item.Name);
            }

            string html = $@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{item.Name} - Önizleme</title><link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css' rel='stylesheet'></head>
            <body style='background:#202124; color:#fff; display:flex; flex-direction:column; align-items:center; justify-content:center; height:100vh; margin:0; font-family:""Segoe UI"",sans-serif;'>
                <div style='background:#303134; padding:40px 60px; border-radius:12px; text-align:center; box-shadow:0 10px 30px rgba(0,0,0,0.5);'>
                    <i class='fas fa-file' style='font-size:64px; color:#8ab4f8; margin-bottom:20px;'></i>
                    <h2 style='margin:0 0 10px 0; font-weight:500;'>{item.Name}</h2>
                    <p style='color:#9aa0a6; margin:0 0 20px 0;'>Sahibi: {item.Owner} &bull; Boyut: {item.Size}</p>
                    <div style='padding:15px; background:#202124; border-radius:8px; color:#e8eaed; font-size:14px; margin-top:20px;'>
                        Bu bir sistem simülasyon dosyasıdır. <br>Kendi yüklediğiniz gerçek dosyaların içeriği burada görünecektir.
                    </div>
                </div>
            </body></html>";
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        [HttpGet]
        public IActionResult DownloadFile(string id)
        {
            var item = GetDatabase().FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();

            if (!string.IsNullOrEmpty(item.FileData))
            {
                byte[] bytes = Convert.FromBase64String(item.FileData);
                return File(bytes, item.ContentType, item.Name);
            }

            string contentType = "application/octet-stream";
            if (item.Extension == "pdf") contentType = "application/pdf";
            else if (item.Extension == "xlsx") contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            else if (item.Extension == "png" || item.Extension == "jpg") contentType = "image/" + item.Extension;
            else if (item.Extension == "zip") contentType = "application/zip";

            byte[] dummyBytes = Encoding.UTF8.GetBytes("Bu bir CoreDrive simulasyon dosyasidir.");
            return File(dummyBytes, contentType, item.Name);
        }

        [HttpPost]
        public IActionResult ShareItem(string id, string email, string returnUrl)
        {
            var db = GetDatabase();
            var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null && !string.IsNullOrEmpty(email))
            {
                item.IsShared = true;
                item.SharedWith = email;
                SaveDatabase(db);

                try
                {
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential("seninmailin@gmail.com", "uygulamasifresi"),
                        EnableSsl = true,
                    };
                    var mailMessage = new MailMessage { From = new MailAddress("seninmailin@gmail.com"), Subject = "CoreDrive: Sizinle bir dosya paylaşıldı", Body = $"Merhaba,\n\nCoreDrive platformu üzerinden sizinle bir dosya paylaşıldı.\n\nDosya Adı: {item.Name}\nPaylaşan: {User.Identity.Name}\n\nDosyayı görüntülemek veya indirmek için CoreDrive hesabınıza giriş yapın.", IsBodyHtml = false, };
                    mailMessage.To.Add(email);
                    smtpClient.Send(mailMessage);
                }
                catch { }
            }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost]
        public IActionResult MoveToTrash(string id, string returnUrl)
        {
            var db = GetDatabase();
            var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsDeleted = true; SaveDatabase(db); }
            return Redirect(returnUrl ?? "/Drive/Dashboard");
        }

        [HttpPost]
        public IActionResult RestoreFromTrash(string id)
        {
            var db = GetDatabase();
            var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsDeleted = false; SaveDatabase(db); }
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult DeletePermanently(string id)
        {
            var db = GetDatabase();
            var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null) { db.Remove(item); SaveDatabase(db); }
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult EmptyTrash()
        {
            var db = GetDatabase();
            db.RemoveAll(i => i.IsDeleted);
            SaveDatabase(db);
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult UnshareItem(string id)
        {
            var db = GetDatabase();
            var item = db.FirstOrDefault(i => i.Id == id);
            if (item != null) { item.IsShared = false; SaveDatabase(db); }
            return RedirectToAction("Shared");
        }

        [HttpGet]
        public IActionResult Shared() { return View(GetDatabase().Where(i => i.IsShared && !i.IsDeleted).ToList()); }

        [HttpGet]
        public IActionResult Trash() { return View(GetDatabase().Where(i => i.IsDeleted).ToList()); }
    }
}