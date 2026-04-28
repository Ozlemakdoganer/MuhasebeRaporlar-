using Microsoft.AspNetCore.Mvc;
using LogoRaporApp.Models;
using LogoRaporApp.Services;

namespace LogoRaporApp.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly RoleService _roleService;

        public UserController(UserService userService, RoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("role") == "Admin";
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("user"));
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin()) return Content("Bu sayfaya erişim yetkiniz yok.");

            var users = _userService.GetAllUsers();
            ViewBag.Roles = _roleService.GetAllRoles();
            return View(users);
        }

        [HttpPost]
        public IActionResult AddUser(string username, string password, string fullName, string role)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var user = new User
            {
                Username = username,
                PasswordHash = password, // AddUser içinde hash'lenecek
                FullName = fullName,
                Role = role
            };

            var result = _userService.AddUser(user);

            return Content(result ? "SUCCESS" : "DUPLICATE");
        }

        [HttpPost]
        public IActionResult DeleteUser(string username)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            // Kendini silemesin
            var currentUser = HttpContext.Session.GetString("user");
            if (username == currentUser)
                return Content("SELF_DELETE");

            var result = _userService.DeleteUser(username);
            return Content(result ? "SUCCESS" : "NOT_FOUND");
        }

        [HttpPost]
        public IActionResult UpdateUser(string username, string fullName, string role)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var result = _userService.UpdateUser(username, fullName, role);
            return Content(result ? "SUCCESS" : "NOT_FOUND");
        }

        [HttpPost]
        public IActionResult ChangePassword(string username, string newPassword)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var result = _userService.ChangePassword(username, newPassword);
            return Content(result ? "SUCCESS" : "NOT_FOUND");
        }
    }
}