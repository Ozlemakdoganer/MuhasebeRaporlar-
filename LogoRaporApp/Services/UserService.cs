using System.Text.Json;
using LogoRaporApp.Models;
using Microsoft.AspNetCore.Hosting;

namespace LogoRaporApp.Services
{
    public class UserService
    {
        private readonly string _filePath;

        public UserService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "users.json");
        }

        private List<User> ReadUsers()
        {
            if (!File.Exists(_filePath))
                return new List<User>();

            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<UserFileModel>(json);
            return data?.Users ?? new List<User>();
        }

        private void WriteUsers(List<User> users)
        {
            var data = new UserFileModel { Users = users };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public User? ValidateUser(string username, string password)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public List<User> GetAllUsers()
        {
            return ReadUsers();
        }

        public bool AddUser(User user)
        {
            var users = ReadUsers();

            // Aynı kullanıcı adı var mı?
            if (users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            users.Add(user);
            WriteUsers(users);
            return true;
        }

        public bool DeleteUser(string username)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return false;

            users.Remove(user);
            WriteUsers(users);
            return true;
        }

        public bool ChangePassword(string username, string newPassword)
        {
            var users = ReadUsers();
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            WriteUsers(users);
            return true;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }

    public class UserFileModel
    {
        public List<User> Users { get; set; } = new();
    }
}