using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace LogoRaporApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "123")
            {
                HttpContext.Session.SetString("user", username);
                return RedirectToAction("Settings", "Home");
            }

            ViewBag.Hata = "Kullanıcı adı veya şifre yanlış";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
