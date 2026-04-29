using LogoRaporApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace LogoRaporApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;
        private readonly LoginProtectionService _loginProtection;

        public AccountController(
            IConfiguration configuration,
            UserService userService,
            LoginProtectionService loginProtection)
        {
            _configuration = configuration;
            _userService = userService;
            _loginProtection = loginProtection;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            System.Diagnostics.Debug.WriteLine($"LOGIN ATTEMPT: {username}");
            // Brute force kontrolü
            if (_loginProtection.IsLocked(username))
            {
                var remaining = _loginProtection.RemainingMinutes(username);
                ViewBag.Hata = $"Çok fazla başarısız deneme. {remaining} dakika bekleyiniz.";
                return View();
            }

            var connStr = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
            {
                ViewBag.Hata = "Veritabanı bağlantısı yapılandırılmamış. Lütfen sistem yöneticisiyle iletişime geçin.";
                return View();
            }

            var user = _userService.ValidateUser(username, password);

            if (user != null)
            {
                _loginProtection.ResetAttempts(username);

                HttpContext.Session.SetString("user", user.Username);
                HttpContext.Session.SetString("fullName", user.FullName);
                HttpContext.Session.SetString("role", user.Role);
                HttpContext.Session.SetString("db", connStr);

                return RedirectToAction("Dashboard", "Home");
            }

            _loginProtection.RecordFailedAttempt(username);
            ViewBag.Hata = "Kullanıcı adı veya şifre yanlış";
            return View();
        }

        [HttpGet]
        public IActionResult ChangeMyPassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("user")))
                return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public IActionResult ChangeMyPassword(string currentPassword, string newPassword)
        {
            var username = HttpContext.Session.GetString("user");

            if (string.IsNullOrEmpty(username))
                return Content("LOGIN");

            var user = _userService.ValidateUser(username, currentPassword);

            if (user == null)
                return Content("Mevcut şifre yanlış.");

            _userService.ChangePassword(username, newPassword);
            return Content("SUCCESS");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return RedirectToAction("Login", "Account");
        }
    }
}