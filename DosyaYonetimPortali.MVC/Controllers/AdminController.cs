using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

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
    }
}