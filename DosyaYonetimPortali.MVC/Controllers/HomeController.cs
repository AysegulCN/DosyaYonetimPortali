using Microsoft.AspNetCore.Mvc;

namespace DosyaYonetimPortali.MVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(bool preview = false)
        {
            if (!preview && User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Dashboard", "Drive");
            }
            return View();
        }
    }
}