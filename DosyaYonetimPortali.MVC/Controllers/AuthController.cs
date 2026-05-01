using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt; // Bu kütüphane Token'ı çözmek için şarttır

namespace DosyaYonetimPortali.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Home/Index.cshtml", model);

            var client = _httpClientFactory.CreateClient();

            // DİKKAT: Buradaki 7145 portu, senin API projeni çalıştırdığındaki port olmalıdır.
            // Eğer senin API farklı bir portta çalışıyorsa burayı güncellemelisin.
            var apiUrl = "https://localhost:7145/api/Auth/login";

            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<TokenResponseModel>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (tokenData != null && !string.IsNullOrEmpty(tokenData.Token))
                {
                    // 1. JWT Token'ı Parçala ve İçindeki Bilgileri (Claims) Al
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(tokenData.Token);
                    var claims = jwtToken.Claims.ToList();

                    // İleride API'ye dosya yüklerken kullanmak üzere Token'ın kendisini de çantaya koyuyoruz
                    claims.Add(new Claim("access_token", tokenData.Token));

                    // 2. MVC Sistemine Kimliği Tanıt ve Giriş Yaptır (Cookie Oluştur)
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = jwtToken.ValidTo
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 3. ROL KONTROLÜ VE YÖNLENDİRME (İşte Zekanın Konuştuğu Yer!)
                    if (claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin"); // Admin ise Yönetim Paneline
                    }

                    return RedirectToAction("Dashboard", "Drive"); // Normal User ise Drive'a
                }
            }

            // Hata varsa tekrar mavi ekrana (Giriş sayfasına) döndür
            ModelState.AddModelError(string.Empty, "E-posta veya şifreniz hatalı. Lütfen tekrar deneyin.");
            return View("~/Views/Home/Index.cshtml", model);
        }

        // Çıkış Yapma Metodu
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Home/Register.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Home/Register.cshtml", model);

            var client = _httpClientFactory.CreateClient();
            var apiUrl = "https://localhost:7145/api/Auth/register"; // API Portunun aynı olduğundan emin ol!

            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // Kayıt başarılıysa direkt Login sayfasına yönlendir (Kullanıcı kendi giriş yapsın)
                return RedirectToAction("Index", "Home");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, "Kayıt olurken bir hata oluştu. Şifrenizin en az 6 karakter olduğuna emin olun.");
            return View("~/Views/Home/Register.cshtml", model);
        }
    }

    // API'den gelen veriyi tutacak minik yardımcı sınıf (Aynı dosyanın en altında durabilir)
    public class TokenResponseModel
    {
        public string Token { get; set; }
        public bool IsSuccessful { get; set; }
    }
}