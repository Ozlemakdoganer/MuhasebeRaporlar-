using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using LogoRaporApp.Models;
using System.Data;
using System.Linq;


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

            ViewBag.DbConnected = !string.IsNullOrEmpty(HttpContext.Session.GetString("db"));
            ViewBag.SelectedFirm = HttpContext.Session.GetInt32("firm");
            ViewBag.SelectedPeriod = HttpContext.Session.GetInt32("period");
            ViewBag.NeedFirmSelection =
                !string.IsNullOrEmpty(HttpContext.Session.GetString("db")) &&
                (HttpContext.Session.GetInt32("firm") == null || HttpContext.Session.GetInt32("period") == null);

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

                bool hasExistingSelection =
    HttpContext.Session.GetInt32("firm") != null &&
    HttpContext.Session.GetInt32("period") != null;

                HttpContext.Session.SetString("db", connStr);
                HttpContext.Session.Remove("firm");
                HttpContext.Session.Remove("period");

                if (hasExistingSelection)
                {
                    return Content("SUCCESS_UPDATE");
                }

                return Content("SUCCESS_FIRST");



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
                ViewBag.SelectedFirm = HttpContext.Session.GetInt32("firm");
                ViewBag.SelectedPeriod = HttpContext.Session.GetInt32("period");

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
                        "SELECT NR, BEGDATE, ENDDATE \r\nFROM L_CAPIPERIOD \r\nWHERE FIRMNR = @firmNr",
                        con);

                    cmd.Parameters.AddWithValue("@firmNr", firmNr);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        donemler.Add(new
                        {
                            periodNr = dr["NR"],
                            begDate = Convert.ToDateTime(dr["BEGDATE"]).ToString("dd.MM.yyyy"),
                            endDate = Convert.ToDateTime(dr["ENDDATE"]).ToString("dd.MM.yyyy")
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



        //--------MIZAN-----------        
        [HttpGet]
        public IActionResult Mizan(
    string? baslangicTarihi,
    string? bitisTarihi,
    string? hesapKoduBaslangic,
    string? hesapKoduBitis,
    string? hesapSeviyesi,
    string? hesapTuru,
    string? hareketGormeyenler,
    string? bakiyeVermeyenler)
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB bağlantısı bulunamadı.");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (firm == null || period == null)
                return Content("Firma / dönem seçimi yapılmamış.");

            ViewBag.BaslangicTarihi = baslangicTarihi;
            ViewBag.BitisTarihi = bitisTarihi;
            ViewBag.HesapKoduBaslangic = hesapKoduBaslangic;
            ViewBag.HesapKoduBitis = hesapKoduBitis;
            ViewBag.HesapSeviyesi = hesapSeviyesi;
            ViewBag.HesapTuru = hesapTuru;
            ViewBag.HareketGormeyenler = hareketGormeyenler;
            ViewBag.BakiyeVermeyenler = bakiyeVermeyenler;

            List<MizanItem> model = new List<MizanItem>();
            Dictionary<string, (decimal Borc, decimal Alacak)> hareketToplamlari = new();

            var connStr = HttpContext.Session.GetString("db");
            string firmStr = firm.Value.ToString("D3");
            string periodStr = period.Value.ToString("D2");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string hareketSql = $@"
    SELECT ACCOUNTCODE, SUM(DEBIT) AS TOPLAM_BORC, SUM(CREDIT) AS TOPLAM_ALACAK
    FROM LG_{firmStr}_{periodStr}_EMFLINE
    GROUP BY ACCOUNTCODE
";

                SqlCommand hareketCmd = new SqlCommand(hareketSql, con);
                SqlDataReader hareketDr = hareketCmd.ExecuteReader();

                while (hareketDr.Read())
                {
                    string hesapKodu = hareketDr["ACCOUNTCODE"]?.ToString() ?? "";
                    decimal borc = hareketDr["TOPLAM_BORC"] != DBNull.Value ? Convert.ToDecimal(hareketDr["TOPLAM_BORC"]) : 0;
                    decimal alacak = hareketDr["TOPLAM_ALACAK"] != DBNull.Value ? Convert.ToDecimal(hareketDr["TOPLAM_ALACAK"]) : 0;

                    var ustKodlar = HesapKoduKir(hesapKodu);

                    foreach (var kod in ustKodlar)
                    {
                        if (hareketToplamlari.ContainsKey(kod))
                        {
                            var mevcut = hareketToplamlari[kod];
                            hareketToplamlari[kod] = (mevcut.Borc + borc, mevcut.Alacak + alacak);
                        }
                        else
                        {
                            hareketToplamlari[kod] = (borc, alacak);
                        }
                    }
                }


                hareketDr.Close();


                string sql = $@"
        SELECT CODE, DEFINITION_
        FROM LG_{firmStr}_EMUHACC
        ORDER BY CODE
    ";

                SqlCommand cmd = new SqlCommand(sql, con);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string hesapKodu = dr["CODE"]?.ToString() ?? "";
                    string hesapAdi = dr["DEFINITION_"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(hesapSeviyesi))
                    {
                        int secilenSeviye = Convert.ToInt32(hesapSeviyesi);
                        int mevcutSeviye = HesapSeviyesiBul(hesapKodu);

                        if (mevcutSeviye > secilenSeviye)
                            continue;
                    }


                    decimal borc = 0;
                    decimal alacak = 0;

                    if (hareketToplamlari.ContainsKey(hesapKodu))
                    {
                        borc = hareketToplamlari[hesapKodu].Borc;
                        alacak = hareketToplamlari[hesapKodu].Alacak;
                    }

                    decimal borcBakiye = 0;
                    decimal alacakBakiye = 0;

                    if (borc > alacak)
                    {
                        borcBakiye = borc - alacak;
                    }
                    else if (alacak > borc)
                    {
                        alacakBakiye = alacak - borc;
                    }

                    model.Add(new MizanItem
                    {
                        HesapKodu = hesapKodu,
                        HesapAdi = hesapAdi,
                        Borc = borc,
                        Alacak = alacak,
                        BorcBakiye = borcBakiye,
                        AlacakBakiye = alacakBakiye
                    });



                }
            }


            return View(model);
        }
        //--------------Kırılım-----
        private List<string> HesapKoduKir(string hesapKodu)
        {
            List<string> sonuc = new List<string>();

            if (string.IsNullOrWhiteSpace(hesapKodu))
                return sonuc;

            var parcalar = hesapKodu.Split('.');

            for (int i = 0; i < parcalar.Length; i++)
            {
                sonuc.Add(string.Join(".", parcalar.Take(i + 1)));
            }

            return sonuc;
        }
        //-------HesapKoduKır-----------

        private int HesapSeviyesiBul(string hesapKodu)
        {
            if (string.IsNullOrWhiteSpace(hesapKodu))
                return 0;

            return hesapKodu.Split('.').Length;
        }


        // ---------------- DASHBOARD ----------------

        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("user") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.DbConnected = !string.IsNullOrEmpty(HttpContext.Session.GetString("db"));
            ViewBag.CanUseReports =
                !string.IsNullOrEmpty(HttpContext.Session.GetString("db")) &&
                HttpContext.Session.GetInt32("firm") != null &&
                HttpContext.Session.GetInt32("period") != null;

            return View();
        }


        //---------DATABASE SETTINGS------------

        
        [HttpGet]
        public IActionResult DbSettings()
        {
            if (HttpContext.Session.GetString("user") == null)
                return RedirectToAction("Login", "Account");

            var model = new DbSetting();

            var connStr = HttpContext.Session.GetString("db");

            if (!string.IsNullOrEmpty(connStr))
            {
                var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                        model.Server = part.Substring("Server=".Length);

                    if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                        model.Database = part.Substring("Database=".Length);

                    if (part.StartsWith("User Id=", StringComparison.OrdinalIgnoreCase))
                        model.Username = part.Substring("User Id=".Length);
                }
            }

            return View(model);
        }

        [HttpGet]
        //---------SATIS FATURALARI ACTION---------
        public IActionResult SatisFaturalari()
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB bağlantısı bulunamadı.");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (firm == null || period == null)
                return Content("Firma / dönem seçimi yapılmamış.");

            return View();
        }

        //--------sayfalarda firma-dönem bilgi çek-----------------
        public JsonResult GetSessionInfo()
        {
            var connStr = HttpContext.Session.GetString("db");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (string.IsNullOrEmpty(connStr) || firm == null || period == null)
            {
                return Json(new { });
            }

            string firmStr = firm.Value.ToString("D3");

            string firmaAdi = "";
            string begDate = "";
            string endDate = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // 🔹 Firma adı
                SqlCommand cmd1 = new SqlCommand(
                    "SELECT NAME FROM L_CAPIFIRM WHERE NR = @nr", con);

                cmd1.Parameters.AddWithValue("@nr", firm.Value);

                var result1 = cmd1.ExecuteScalar();
                firmaAdi = Convert.ToString(result1) ?? "";

                // 🔹 Dönem tarihleri
                SqlCommand cmd2 = new SqlCommand(
                    "SELECT BEGDATE, ENDDATE FROM L_CAPIPERIOD WHERE FIRMNR=@f AND NR=@p", con);

                cmd2.Parameters.AddWithValue("@f", firm.Value);
                cmd2.Parameters.AddWithValue("@p", period.Value);

                var dr = cmd2.ExecuteReader();

                if (dr.Read())
                {
                    begDate = Convert.ToDateTime(dr["BEGDATE"]).ToString("dd.MM.yyyy");
                    endDate = Convert.ToDateTime(dr["ENDDATE"]).ToString("dd.MM.yyyy");
                }
            }

            return Json(new
            {
                firmNr = firm.Value.ToString("D3"),
                firmaAdi,
                periodNr = period.Value.ToString("D2"),
                begDate,
                endDate
            });
        }

        //-----------------Mizan--------
       
    }
}