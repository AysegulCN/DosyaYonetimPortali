using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize] // Sadece giriş yapan kullanıcılar girebilir
    public class DriveController : Controller
    {
        // 1. Ana Dosyalarım (Drive) Ekranı
        public IActionResult Dashboard()
        {
            return View();
        }

        // 2. Benimle Paylaşılan Dosyalar
        public IActionResult Shared()
        {
            return View();
        }

        // 3. Çöp Kutusu (Geri Dönüşüm)
        public IActionResult Trash()
        {
            return View();
        }
    }
}