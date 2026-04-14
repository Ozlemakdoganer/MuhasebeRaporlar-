using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using LogoRaporApp.Models;
using System.Data;
using System.Linq;
using System.IO;
using ClosedXML.Excel;





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
            List<string> tumHesapKodlari = new List<string>();

            var connStr = HttpContext.Session.GetString("db");
            string firmStr = firm.Value.ToString("D3");
            string periodStr = period.Value.ToString("D2");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string hareketSql = $@"
            SELECT ACCOUNTCODE, SUM(DEBIT) AS TOPLAM_BORC, SUM(CREDIT) AS TOPLAM_ALACAK
            FROM LG_{firmStr}_{periodStr}_EMFLINE
            WHERE (@baslangicTarihi IS NULL OR DATE_ >= @baslangicTarihi)
              AND (@bitisTarihi IS NULL OR DATE_ <= @bitisTarihi)
            GROUP BY ACCOUNTCODE
        ";

                SqlCommand hareketCmd = new SqlCommand(hareketSql, con);
                hareketCmd.Parameters.AddWithValue("@baslangicTarihi",
                    string.IsNullOrEmpty(baslangicTarihi) ? DBNull.Value : Convert.ToDateTime(baslangicTarihi));
                hareketCmd.Parameters.AddWithValue("@bitisTarihi",
                    string.IsNullOrEmpty(bitisTarihi) ? DBNull.Value : Convert.ToDateTime(bitisTarihi));

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
            WHERE (@hesapKoduBaslangic IS NULL OR @hesapKoduBaslangic = '' OR CODE >= @hesapKoduBaslangic)
              AND (@hesapKoduBitis IS NULL OR @hesapKoduBitis = '' OR CODE <= @hesapKoduBitis)
            ORDER BY CODE
        ";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@hesapKoduBaslangic", (object?)hesapKoduBaslangic ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hesapKoduBitis", (object?)hesapKoduBitis ?? DBNull.Value);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string hesapKodu = dr["CODE"]?.ToString() ?? "";
                    string hesapAdi = dr["DEFINITION_"]?.ToString() ?? "";

                    // Tüm hesap kodlarını listeye ekle
                    tumHesapKodlari.Add(hesapKodu);

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

                    if (!string.IsNullOrEmpty(hesapTuru) && hesapTuru != "Tumu")
                    {
                        bool uygunMu = false;

                        if (hesapTuru == "KdvHesaplari")
                        {
                            uygunMu = hesapKodu.StartsWith("190") ||
                                      hesapKodu.StartsWith("191") ||
                                      hesapKodu.StartsWith("391");
                        }
                        else if (hesapTuru == "Kkeg")
                        {
                            uygunMu = hesapKodu.StartsWith("689");
                        }
                        else if (hesapTuru == "GelirTablosu")
                        {
                            uygunMu = hesapKodu.StartsWith("600") ||
                                      hesapKodu.StartsWith("7");
                        }

                        if (!uygunMu)
                            continue;
                    }

                    if (hareketGormeyenler == "Listelenmeyecek" && borc == 0 && alacak == 0)
                        continue;

                    if (bakiyeVermeyenler == "Listelenmeyecek" && borcBakiye == 0 && alacakBakiye == 0)
                        continue;

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

                dr.Close();
            }

            decimal toplamBorc = 0;
            decimal toplamAlacak = 0;
            decimal toplamBorcBakiye = 0;
            decimal toplamAlacakBakiye = 0;

            foreach (var item in model)
            {
                if (!AltHesabiVarMi(item.HesapKodu, tumHesapKodlari))
                {
                    toplamBorc += item.Borc;
                    toplamAlacak += item.Alacak;
                    toplamBorcBakiye += item.BorcBakiye;
                    toplamAlacakBakiye += item.AlacakBakiye;
                }
            }

            ViewBag.ToplamBorc = toplamBorc;
            ViewBag.ToplamAlacak = toplamAlacak;
            ViewBag.ToplamBorcBakiye = toplamBorcBakiye;
            ViewBag.ToplamAlacakBakiye = toplamAlacakBakiye;

            return View(model);
        }

        
        /*------------------Mizan Excel------------------*/

        [HttpGet]
        public IActionResult MizanExcel(
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
            WHERE (@baslangicTarihi IS NULL OR DATE_ >= @baslangicTarihi)
              AND (@bitisTarihi IS NULL OR DATE_ <= @bitisTarihi)
            GROUP BY ACCOUNTCODE
        ";

                SqlCommand hareketCmd = new SqlCommand(hareketSql, con);
                hareketCmd.Parameters.AddWithValue("@baslangicTarihi",
                    string.IsNullOrEmpty(baslangicTarihi) ? DBNull.Value : Convert.ToDateTime(baslangicTarihi));
                hareketCmd.Parameters.AddWithValue("@bitisTarihi",
                    string.IsNullOrEmpty(bitisTarihi) ? DBNull.Value : Convert.ToDateTime(bitisTarihi));

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
            WHERE (@hesapKoduBaslangic IS NULL OR @hesapKoduBaslangic = '' OR CODE >= @hesapKoduBaslangic)
              AND (@hesapKoduBitis IS NULL OR @hesapKoduBitis = '' OR CODE <= @hesapKoduBitis)
            ORDER BY CODE
        ";

                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@hesapKoduBaslangic", (object?)hesapKoduBaslangic ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hesapKoduBitis", (object?)hesapKoduBitis ?? DBNull.Value);

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

                    if (!string.IsNullOrEmpty(hesapTuru) && hesapTuru != "Tumu")
                    {
                        bool uygunMu = false;

                        if (hesapTuru == "KdvHesaplari")
                        {
                            uygunMu = hesapKodu.StartsWith("190") ||
                                      hesapKodu.StartsWith("191") ||
                                      hesapKodu.StartsWith("391");
                        }
                        else if (hesapTuru == "Kkeg")
                        {
                            uygunMu = hesapKodu.StartsWith("689");
                        }
                        else if (hesapTuru == "GelirTablosu")
                        {
                            uygunMu = hesapKodu.StartsWith("600") ||
                                      hesapKodu.StartsWith("7");
                        }

                        if (!uygunMu)
                            continue;
                    }

                    if (hareketGormeyenler == "Listelenmeyecek" && borc == 0 && alacak == 0)
                        continue;

                    if (bakiyeVermeyenler == "Listelenmeyecek" && borcBakiye == 0 && alacakBakiye == 0)
                        continue;

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

                dr.Close();
            }

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Mizan");

                // Ana başlık
                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Value = "İKİ TARİH ARASI MİZAN";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 16;
                ws.Cell("A1").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                ws.Cell("A1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1F7A5A");
                ws.Cell("A1").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                // Filtre bilgisi
                ws.Cell("A3").Value = "Başlangıç Tarihi";
                ws.Cell("B3").Value = baslangicTarihi;
                ws.Cell("C3").Value = "Bitiş Tarihi";
                ws.Cell("D3").Value = bitisTarihi;

                // Kolon başlıkları
                ws.Cell("A5").Value = "Hesap Kodu";
                ws.Cell("B5").Value = "Hesap Adı";
                ws.Cell("C5").Value = "Borç";
                ws.Cell("D5").Value = "Alacak";
                ws.Cell("E5").Value = "Borç Bakiye";
                ws.Cell("F5").Value = "Alacak Bakiye";

                var headerRange = ws.Range("A5:F5");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2E8B57");
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                decimal toplamBorc = 0;
                decimal toplamAlacak = 0;
                decimal toplamBorcBakiye = 0;
                decimal toplamAlacakBakiye = 0;


                int row = 6;
                foreach (var item in model)
                {
                    ws.Cell(row, 1).Value = item.HesapKodu;
                    ws.Cell(row, 2).Value = item.HesapAdi;
                    ws.Cell(row, 3).Value = item.Borc;
                    ws.Cell(row, 4).Value = item.Alacak;
                    ws.Cell(row, 5).Value = item.BorcBakiye;
                    ws.Cell(row, 6).Value = item.AlacakBakiye;

                    toplamBorc += item.Borc;
                    toplamAlacak += item.Alacak;
                    toplamBorcBakiye += item.BorcBakiye;
                    toplamAlacakBakiye += item.AlacakBakiye;

                    ws.Range(row, 1, row, 6).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    ws.Range(row, 1, row, 6).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                    row++;
                }

                ws.Cell(row, 1).Value = "TOPLAM";
                ws.Range(row, 1, row, 2).Merge();

                ws.Cell(row, 3).Value = toplamBorc;
                ws.Cell(row, 4).Value = toplamAlacak;
                ws.Cell(row, 5).Value = toplamBorcBakiye;
                ws.Cell(row, 6).Value = toplamAlacakBakiye;

                var toplamRange = ws.Range(row, 1, row, 6);
                toplamRange.Style.Font.Bold = true;
                toplamRange.Style.Font.FontColor = XLColor.White;
                toplamRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#176347");
                toplamRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                toplamRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;



                // Para formatı
                ws.Range(6, 3, row, 6).Style.NumberFormat.Format = "#,##0.00";

                // Kolon genişliği
                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"MizanRaporu_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        /*------------------Mizan Excel Sonu------------------*/
        private bool AltHesabiVarMi(string hesapKodu, List<string> tumKodlar)
        {
            return tumKodlar.Any(k => k.StartsWith(hesapKodu + "."));
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
                       
    }
}