using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.MVC.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}