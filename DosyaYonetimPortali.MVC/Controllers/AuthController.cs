using Microsoft.AspNetCore.Mvc;
using DosyaYonetimPortali.MVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DosyaYonetimPortali.MVC.Data;
using DosyaYonetimPortali.MVC.Models.Entities;

namespace DosyaYonetimPortali.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
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
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "192.168.1.15";
            var browserInfo = "Chrome / Windows 11";

            var foundUser = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

            if (foundUser != null)
            {
               

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, $"{foundUser.FirstName} {foundUser.LastName}"),
                    new Claim(ClaimTypes.Email, foundUser.Email),
                    new Claim(ClaimTypes.Role, foundUser.Role) 
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

                _context.LoginRecords.Add(new LoginRecord { UserEmail = foundUser.Email, IpAddress = ipAddress, BrowserDevice = browserInfo, Status = "Başarılı", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = foundUser.Email, Message = "Sisteme başarılı giriş yapıldı.", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
                _context.SaveChanges();

                if (foundUser.Role == "Admin")
                    return RedirectToAction("Dashboard", "Admin");

                return RedirectToAction("Dashboard", "Drive");
            }

            _context.LoginRecords.Add(new LoginRecord { UserEmail = model.Email, IpAddress = ipAddress, BrowserDevice = browserInfo, Status = "Hatalı Şifre", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
            _context.SystemLogs.Add(new SystemLog { Status = "WARN", UserEmail = model.Email, Message = "Sisteme hatalı giriş denemesi yapıldı.", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
            _context.SaveChanges();

            ModelState.AddModelError(string.Empty, "E-posta veya şifreniz hatalı. Lütfen tekrar deneyin.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Dashboard", "Drive");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string FullName, string Email, string Password)
        {
            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError(string.Empty, "Lütfen tüm alanları doldurun.");
                return View();
            }

            if (_context.Users.Any(u => u.Email == Email))
            {
                ModelState.AddModelError(string.Empty, "Bu e-posta adresi zaten kullanılıyor.");
                return View();
            }

            string assignedRole = "User";

            string firstName = FullName.Split(' ').First();
            string defaultAvatar = $"https://ui-avatars.com/api/?name={firstName}&background=random&color=fff&rounded=true";

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString().Substring(0, 8),
                FirstName = firstName,
                LastName = FullName.Contains(" ") ? FullName.Substring(FullName.IndexOf(" ") + 1) : "",
                Email = Email,
                Password = Password,
                Role = assignedRole,
                ProfilePictureUrl = defaultAvatar
            };

            _context.Users.Add(newUser);
            _context.SystemLogs.Add(new SystemLog { Status = "INFO", UserEmail = "Sistem", Message = $"Yeni kullanıcı kayıt oldu: {Email}", Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
            _context.SaveChanges();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{newUser.FirstName} {newUser.LastName}"),
                new Claim(ClaimTypes.Email, newUser.Email),
                new Claim(ClaimTypes.Role, newUser.Role) 
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

            return RedirectToAction("Dashboard", "Drive");
        }
    }
}