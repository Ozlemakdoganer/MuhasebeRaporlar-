using Microsoft.AspNetCore.Mvc;
using LogoRaporApp.Models;
using LogoRaporApp.Services;
using System.Text.Json;

namespace LogoRaporApp.Controllers
{
    public class RoleController : Controller
    {
        private readonly RoleService _roleService;

        public RoleController(RoleService roleService)
        {
            _roleService = roleService;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("role") == "Admin";

        private bool IsLoggedIn() =>
            !string.IsNullOrEmpty(HttpContext.Session.GetString("user"));

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin()) return Content("Bu sayfaya erişim yetkiniz yok.");

            var roles = _roleService.GetAllRoles();
            ViewBag.AllReports = _roleService.GetAllReports();
            return View(roles);
        }
        [HttpGet]
        public IActionResult GetRoles()
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var roles = _roleService.GetAllRoles();
            return Json(roles.Select(r => r.Name).ToList());
        }

        [HttpPost]
        public IActionResult AddRole(string name)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            if (string.IsNullOrWhiteSpace(name))
                return Content("EMPTY");

            var result = _roleService.AddRole(new Role { Name = name });
            return Content(result ? "SUCCESS" : "DUPLICATE");
        }

        [HttpPost]
        public IActionResult UpdateRole(string name, string permissionsJson)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var permissions = JsonSerializer.Deserialize<List<string>>(permissionsJson) ?? new List<string>();

            var result = _roleService.UpdateRole(new Role
            {
                Name = name,
                Permissions = permissions
            });

            return Content(result ? "SUCCESS" : "NOT_FOUND");
        }

        [HttpPost]
        public IActionResult DeleteRole(string name)
        {
            if (!IsLoggedIn()) return Content("LOGIN");
            if (!IsAdmin()) return Content("UNAUTHORIZED");

            var result = _roleService.DeleteRole(name);
            return Content(result ? "SUCCESS" : "NOT_FOUND");
        }
    }
}