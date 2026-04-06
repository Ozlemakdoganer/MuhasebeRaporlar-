using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using LogoRaporApp.Models;
using System.Data;

namespace LogoRaporApp.Controllers
{
    public class HomeController : Controller
    {
        
        // ---------------- SETTINGS (GET) ----------------

        [HttpGet]
        public IActionResult Settings()
        {
            if (HttpContext.Session.GetString("user") == null)
                return RedirectToAction("Login");

            return View();
        }

        // ---------------- SETTINGS (POST) ----------------

        /* [HttpPost]
         public IActionResult Settings(DbSetting model)
         {
             if (HttpContext.Session.GetString("user") == null)
                 return RedirectToAction("Login");


             string connStr =
                 $"Server={model.Server};Database={model.Database};User Id={model.Username};Password={model.Password};TrustServerCertificate=True;";

             try
             {
                 using (var conn = new SqlConnection(connStr))
                 {
                     conn.Open();
                 }

                 HttpContext.Session.SetString("db", connStr);
                 TempData["Success"] = "Veritabanı bağlantısı başarıyla kaydedildi.";
                 return RedirectToAction("Dashboard");
             }
             catch (Exception)
             {
                 ViewBag.Error = "Bağlantı başarısız. Girdiğiniz veritabanı bilgilerini kontrol edip tekrar deneyin.";
                 return View("DbSettings");
             }
         }*/
        [HttpPost]
        public IActionResult SaveDbSettings(DbSetting model)
        {
            if (HttpContext.Session.GetString("user") == null)
                return Content("LOGIN");

            string connStr =
                $"Server={model.Server};Database={model.Database};User Id={model.Username};Password={model.Password};TrustServerCertificate=True;";

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                }

                HttpContext.Session.SetString("db", connStr);

                return Content("SUCCESS");
            }
            catch (Exception)
            {
                ViewBag.Error = "Bağlantı başarısız. Girdiğiniz veritabanı bilgilerini kontrol edip tekrar deneyin.";
                return View("DbSettings");
            }
        }



        // ---------------- SELECT COMPANY ----------------

        [HttpGet]
        public IActionResult SelectCompany()
        {
            var connStr = HttpContext.Session.GetString("db");

            if (string.IsNullOrEmpty(connStr))
                return RedirectToAction("Settings");

            List<Firma> firmalar = new List<Firma>();

            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand("SELECT TOP 50 NR, NAME FROM L_CAPIFIRM", con);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            firmalar.Add(new Firma
                            {
                                Nr = Convert.ToInt32(dr["NR"]),
                                Name = dr["NAME"]?.ToString() ?? ""
                            });
                        }
                    }
                }

                ViewBag.Firmalar = firmalar;
                return View();
            }
            catch (Exception ex)
            {
                return Content("HATA: " + ex.Message);
            }
        }

        //--------------SELECT PERIOD--------------------

        [HttpGet]
        public JsonResult GetPeriods(int firmNr)
        {
            var connStr = HttpContext.Session.GetString("db");

            List<object> donemler = new List<object>();

            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand(
                        "SELECT NR FROM L_CAPIPERIOD WHERE FIRMNR = @firmNr",
                        con);

                    cmd.Parameters.AddWithValue("@firmNr", firmNr);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        donemler.Add(new
                        {
                            periodNr = dr["NR"]
                        });
                    }
                }

                return Json(donemler);
            }
            catch (Exception ex)
            {
                return Json(new { hata = ex.Message });
            }
        }
        //----------FIRMA+DONEM SECIMI SONRASI-----------------

        [HttpPost]
        public IActionResult SelectCompany(int firmNr, int periodNr)
        {
            HttpContext.Session.SetInt32("firm", firmNr);
            HttpContext.Session.SetInt32("period", periodNr);

            return RedirectToAction("Dashboard");
        }

        //----------------CARI EKSTRE--------------
        public IActionResult CariEkstre(string? arama)
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB yok");

            var connStr = HttpContext.Session.GetString("db");

            if (string.IsNullOrEmpty(connStr))
                return RedirectToAction("Settings");

            var firm = HttpContext.Session.GetInt32("firm");

            if (firm == null)
                return RedirectToAction("SelectCompany");

            string firmStr = firm.Value.ToString("D3");

            List<CariListeItem> cariler = new List<CariListeItem>();


            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();

                    string sql = $@"
    SELECT CODE, DEFINITION_
    FROM LG_{firmStr}_CLCARD
    WHERE (@arama IS NULL OR @arama = '')
       OR CODE LIKE '%' + @arama + '%'
       OR DEFINITION_ LIKE '%' + @arama + '%'
    ORDER BY CODE
";


                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@arama", (object?)arama ?? DBNull.Value);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
{
    cariler.Add(new CariListeItem
    {
        CariKod = dr["CODE"]?.ToString() ?? "",
        Unvan = dr["DEFINITION_"]?.ToString() ?? "",
        Bakiye = 0,
        VknTckn = ""
    });
}


                }
                return View(cariler);

            }
            catch (Exception ex)
            {
                return Content("HATA: " + ex.Message);
            }
        }

        // ---------------- DASHBOARD ----------------

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("user") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.DbConnected = !string.IsNullOrEmpty(HttpContext.Session.GetString("db"));

            return View();
        }

        //---------DATABASE SETTINGS------------

        [HttpGet]
        public IActionResult DbSettings()
        {
            if (HttpContext.Session.GetString("user") == null)
                return RedirectToAction("Login", "Account");

            return View();
        }


    }
}