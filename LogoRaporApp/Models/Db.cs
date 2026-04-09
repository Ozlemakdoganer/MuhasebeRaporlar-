using Microsoft.Data.SqlClient;
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

            var connStr = session.GetString("db");

            if (string.IsNullOrEmpty(connStr))
                throw new Exception("Veritabanı bağlantısı bulunamadı!");

            return new SqlConnection(connStr);
        }
    }
}
