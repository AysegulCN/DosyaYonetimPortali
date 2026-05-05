using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

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
            return View();
        }

        public IActionResult Storage()
        {
            return View();
        }

        public IActionResult Logs()
        {
            return View();
        }

        public IActionResult Shares()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RevokeShare(string id)
        {
            TempData["Message"] = "Seçilen dosyanın paylaşım bağlantısı başarıyla iptal edildi.";
            return RedirectToAction("Shares");
        }

        [HttpPost]
        public IActionResult RevokeAllShares()
        {
            TempData["Message"] = "Sistemdeki tüm açık paylaşım bağlantıları başarıyla kapatıldı.";
            return RedirectToAction("Shares");
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
                TempData["ToastMessage"] = $"'{ModuleName}' modülü sistemden kalıcı olarak silindi.";
                TempData["ToastIcon"] = "success";
            }
            return RedirectToAction("Permissions");
        }

        [HttpGet]
        public IActionResult LoginRecords()
        {
            return View();
        }

        [HttpGet]
        public IActionResult FileActivities()
        {
            return View();
        }

        [HttpGet]
        public IActionResult FileSettings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult QuotaManagement()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RefreshStorage()
        {
            TempData["Message"] = "Sunucu depolama verileri güncellendi ve en son durum ekrana yansıtıldı.";
            return RedirectToAction("Storage");
        }

        [HttpPost]
        public IActionResult GenerateSystemReport()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                var header = new Paragraph("CORE-DRIVE SISTEM RAPORU")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(22)
                    .SetBold();
                document.Add(header);

                document.Add(new Paragraph("Rapor Tarihi: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(10)
                    .SetMarginBottom(20));

                document.Add(new Paragraph("---------------------------------------------------------------------------------------------------"));

                document.Add(new Paragraph("Toplam Kullanici: 124").SetFontSize(14).SetMarginBottom(5));
                document.Add(new Paragraph("Yuklenen Toplam Dosya: 3,458").SetFontSize(14).SetMarginBottom(5));
                document.Add(new Paragraph("Kullanilan Depolama: %45 (225 GB / 500 GB)").SetFontSize(14).SetMarginBottom(5));
                document.Add(new Paragraph("Aktif Paylasilan Linkler: 86").SetFontSize(14).SetMarginBottom(20));

                document.Add(new Paragraph("---------------------------------------------------------------------------------------------------"));

                document.Add(new Paragraph("Sistem Durumu: SAGLIKLI").SetBold().SetFontSize(12));
                document.Add(new Paragraph("Guvenlik Taramasi: TEMIZ").SetBold().SetFontSize(12));

                document.Close();

                byte[] fileBytes = ms.ToArray();
                string fileName = $"CoreDrive_Rapor_{DateTime.Now.ToString("yyyyMMdd")}.pdf";

                return File(fileBytes, "application/pdf", fileName);
            }
        }
    }
}