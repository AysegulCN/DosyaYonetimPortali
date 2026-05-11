using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace DosyaYonetimPortali.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
                return RedirectToAction("Dashboard", "Drive");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Home/Index.cshtml", model);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "192.168.1.15";
            var browserInfo = "Chrome / Windows 11";

            string profileDbPath = Path.Combine(Directory.GetCurrentDirectory(), "admin_profile.json");
            string adminEmail = "patron@coredrive.com";
            string adminPass = "aysegul123";
            string adminName = "Sistem Yöneticisi";

            if (System.IO.File.Exists(profileDbPath))
            {
                using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(profileDbPath));
                var root = doc.RootElement;
                if (root.TryGetProperty("Email", out var emailProp)) adminEmail = emailProp.GetString() ?? adminEmail;
                if (root.TryGetProperty("Password", out var passProp)) adminPass = passProp.GetString() ?? adminPass;

                string fName = root.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "Sistem";
                string lName = root.TryGetProperty("LastName", out var ln) ? ln.GetString() : "Yöneticisi";
                adminName = $"{fName} {lName}";
            }

            if ((model.Email == "aysegulcoban@gmail.com" && model.Password == "aysegul123") ||
                (model.Email == adminEmail && model.Password == adminPass))
            {
                string currentName = model.Email == "aysegulcoban@gmail.com" ? "Ayşegül Yılmaz" : adminName;

                var testClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, currentName),
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                var testIdentity = new ClaimsIdentity(testClaims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(testIdentity), new AuthenticationProperties { IsPersistent = true });
                SystemLogger.AddLoginRecord(model.Email, ipAddress, browserInfo, "Başarılı", true);
                SystemLogger.AddLog("INFO", model.Email, "Sisteme başarılı giriş yapıldı.");

                return RedirectToAction("Dashboard", "Admin");
            }

            var client = _httpClientFactory.CreateClient();
            var apiUrl = "https://localhost:7145/api/Auth/login";
            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<TokenResponseModel>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenData != null && !string.IsNullOrEmpty(tokenData.Token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(tokenData.Token);
                    var claims = jwtToken.Claims.ToList();
                    var roleClaim = claims.FirstOrDefault(c => c.Type == "role" || c.Type == "Role" || c.Type == ClaimTypes.Role);
                    if (roleClaim != null && roleClaim.Type != ClaimTypes.Role) claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                    claims.Add(new Claim("access_token", tokenData.Token));

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = jwtToken.ValidTo };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    SystemLogger.AddLoginRecord(model.Email, ipAddress, browserInfo, "Başarılı", true);
                    SystemLogger.AddLog("INFO", model.Email, "Sisteme başarılı giriş yapıldı.");

                    if (claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase))) return RedirectToAction("Dashboard", "Admin");
                    return RedirectToAction("Dashboard", "Drive");
                }
            }

            SystemLogger.AddLoginRecord(model.Email, ipAddress, browserInfo, "Hatalı Şifre", false);
            SystemLogger.AddLog("WARN", model.Email, "Sisteme hatalı giriş denemesi yapıldı.");
            ModelState.AddModelError(string.Empty, "E-posta veya şifreniz hatalı. Lütfen tekrar deneyin.");
            return View("~/Views/Home/Index.cshtml", model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Drive");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient();
            var apiUrl = "https://localhost:7145/api/Auth/register";
            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, jsonContent);

            if (response.IsSuccessStatusCode) return RedirectToAction("Login", "Auth");

            ModelState.AddModelError(string.Empty, "Kayıt olurken bir hata oluştu. Şifrenizin en az 6 karakter olduğuna emin olun.");
            return View(model);
        }
    }

    public class TokenResponseModel
    {
        public string Token { get; set; }
        public bool IsSuccessful { get; set; }
    }
}