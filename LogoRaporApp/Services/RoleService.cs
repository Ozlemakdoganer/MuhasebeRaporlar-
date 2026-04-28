using System.Text.Json;
using LogoRaporApp.Models;

namespace LogoRaporApp.Services
{
    public class RoleService
    {
        private readonly string _filePath;

        public RoleService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "roles.json");
        }

        private List<Role> ReadRoles()
        {
            if (!File.Exists(_filePath))
                return new List<Role>();

            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<RoleFileModel>(json);
            return data?.Roles ?? new List<Role>();
        }

        private void WriteRoles(List<Role> roles)
        {
            var data = new RoleFileModel { Roles = roles };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public List<Role> GetAllRoles()
        {
            return ReadRoles();
        }

        public Role? GetRole(string name)
        {
            return ReadRoles().FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool AddRole(Role role)
        {
            var roles = ReadRoles();

            if (roles.Any(r => r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
                return false;

            roles.Add(role);
            WriteRoles(roles);
            return true;
        }

        public bool UpdateRole(Role role)
        {
            var roles = ReadRoles();
            var existing = roles.FirstOrDefault(r =>
                r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
                return false;

            existing.Permissions = role.Permissions;
            WriteRoles(roles);
            return true;
        }

        public bool DeleteRole(string name)
        {
            var roles = ReadRoles();
            var role = roles.FirstOrDefault(r =>
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (role == null)
                return false;

            roles.Remove(role);
            WriteRoles(roles);
            return true;
        }

        public bool HasPermission(string roleName, string permission)
        {
            if (roleName == "Admin") return true;

            var role = GetRole(roleName);
            if (role == null) return false;

            return role.Permissions.Contains(permission);
        }

        // Tüm mevcut raporların listesi
        public List<string> GetAllReports()
        {
            return new List<string>
            {
                "CariEkstre",
                "SatisFaturalari",
                "SatinAlmaFaturalari",
                "Mizan",
                "GelirTablosuAnalizi",
                "GelirTablosu",
                "Bilanco",
                "EEnvanter"
            };
        }
    }

    public class RoleFileModel
    {
        public List<Role> Roles { get; set; } = new();
    }
}