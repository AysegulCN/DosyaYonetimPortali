using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.MVC.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        // 3. Sunucu Depolama Analizi Ekranı
        public IActionResult Storage()
        {
            return View();
        }

        // 4. Sistem Logları Ekranı
        public IActionResult Logs()
        {
            return View();
        }
    }
}