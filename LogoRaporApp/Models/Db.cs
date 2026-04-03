using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LogoRaporApp.Models
{
    public class Db
    {
        private readonly IHttpContextAccessor _accessor;

        public Db(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public SqlConnection Baglanti()
        {
            var session = _accessor.HttpContext?.Session;

            if (session == null)
                throw new Exception("Session bulunamadı!");

            var json = session.GetString("db");

            if (string.IsNullOrEmpty(json))
                throw new Exception("Veritabanı ayarları bulunamadı!");

            var setting = JsonSerializer.Deserialize<DbSetting>(json);

            if (setting == null ||
                string.IsNullOrEmpty(setting.Server) ||
                string.IsNullOrEmpty(setting.Database))
            {
                throw new Exception("Db ayarları eksik!");
            }

            string connStr =
                $"Server={setting.Server};Database={setting.Database};User Id={setting.Username};Password={setting.Password};TrustServerCertificate=True;";

            return new SqlConnection(connStr);
        }
    }
}