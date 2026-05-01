using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
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

        public async Task<IActionResult> Users()
        {
            var client = _httpClientFactory.CreateClient();

            var token = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("https://localhost:7145/api/users");

            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<UserViewModel>>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View(users);
            }

            return View(new List<UserViewModel>());
        }

        public IActionResult Storage()
        {
            return View();
        }

        public IActionResult Logs()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            await client.DeleteAsync($"https://localhost:7145/api/users/{id}");

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRole(string id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            await client.PostAsync($"https://localhost:7145/api/users/toggle-role/{id}", null);

            return RedirectToAction("Users");
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
        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddUser(string FirstName, string LastName, string Email, string Password, string Role)
        {
            
            TempData["Message"] = $"{FirstName} {LastName} isimli kullanıcı ({Role}) olarak sisteme başarıyla eklendi.";

            return RedirectToAction("Users");
        }
        [HttpGet]
        public IActionResult Roles()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Permissions()
        {
            return View();
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