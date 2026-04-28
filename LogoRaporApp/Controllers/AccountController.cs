using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using LogoRaporApp.Services;

namespace LogoRaporApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public AccountController(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
            {
                ViewBag.Hata = "Veritabanı bağlantısı yapılandırılmamış. Lütfen sistem yöneticisiyle iletişime geçin.";
                return View();
            }

            // Kullanıcıyı users.json'dan doğrula
            var user = _userService.ValidateUser(username, password);

            if (user != null)
            {
                HttpContext.Session.SetString("user", user.Username);
                HttpContext.Session.SetString("role", user.Role);
                HttpContext.Session.SetString("db", connStr);

                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.Hata = "Kullanıcı adı veya şifre yanlış";
            return View();
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