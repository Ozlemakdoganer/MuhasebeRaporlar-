using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için gerekli

namespace LogoRaporApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Kullanıcı adı ve şifre kontrolü
            // (Burayı şimdilik basit tutacağız, sonra DB'den kontrol ekleriz)
            if (username == "admin" && password == "123") // Örnek kullanıcı adı/şifre
            {
                // Giriş başarılı, session oluştur
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("Username", username);

                // Kullanıcıyı veritabanı bağlantı sayfasına yönlendir
                return RedirectToAction("Settings", "Home");
            }
            else
            {
                // Giriş başarısız, aynı sayfaya geri dön ve hata mesajı göster
                ViewBag.ErrorMessage = "Hatalı kullanıcı adı veya şifre!";
                return View();
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Tüm session'ı temizle
            return RedirectToAction("Login", "Account"); // Giriş sayfasına yönlendir
        }
    }
}
