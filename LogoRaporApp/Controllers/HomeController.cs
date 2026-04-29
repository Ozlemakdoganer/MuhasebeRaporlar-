using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using LogoRaporApp.Models;
using LogoRaporApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;







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

                // Session'ı güncelle
                HttpContext.Session.SetString("db", connStr);
                HttpContext.Session.Remove("firm");
                HttpContext.Session.Remove("period");

                // appsettings.json'ı güncelle
                UpdateConnectionString(connStr);

                if (hasExistingSelection)
                    return Content("SUCCESS_UPDATE");

                return Content("SUCCESS_FIRST");
            }
            catch (Exception)
            {
                ViewBag.Error = "Bağlantı başarısız. Girdiğiniz veritabanı bilgilerini kontrol edip tekrar deneyin.";
                return View("DbSettings", model);
            }
        }
        /*----------------- Update Connection String in appsettings.json -----------------*/

        private void UpdateConnectionString(string newConnStr)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = System.IO.File.ReadAllText(appSettingsPath);

            var jsonObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                          ?? new Dictionary<string, object>();

            var connStrings = new Dictionary<string, string>
    {
        { "DefaultConnection", newConnStr }
    };

            jsonObj["ConnectionStrings"] = connStrings;

            var updatedJson = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            System.IO.File.WriteAllText(appSettingsPath, updatedJson);
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
     string? bakiyeVermeyenler,
     string? kapanisFisi)
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB bağlantısı bulunamadı.");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");
            var connStr = HttpContext.Session.GetString("db");

            if (firm == null || period == null)
                return Content("Firma / dönem seçimi yapılmamış.");
            // Tarihler boşsa dönem tarihlerini otomatik getir
            string donemBaslangic = "";
            string donemBitis = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT BEGDATE, ENDDATE FROM L_CAPIPERIOD WHERE FIRMNR=@f AND NR=@p", con);
                cmd.Parameters.AddWithValue("@f", firm.Value);
                cmd.Parameters.AddWithValue("@p", period.Value);

                var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    var begDate = Convert.ToDateTime(dr["BEGDATE"]);
                    var endDate = Convert.ToDateTime(dr["ENDDATE"]);

                    donemBaslangic = begDate.ToString("yyyy-MM-dd");
                    donemBitis = endDate.ToString("yyyy-MM-dd");

                    if (string.IsNullOrEmpty(baslangicTarihi) || string.IsNullOrEmpty(bitisTarihi))
                    {
                        var bugun = DateTime.Today;
                        if (endDate > bugun)
                            endDate = bugun;

                        baslangicTarihi = begDate.ToString("yyyy-MM-dd");
                        bitisTarihi = endDate.ToString("yyyy-MM-dd");
                    }
                }
            }

            ViewBag.DonemBaslangic = donemBaslangic;
            ViewBag.DonemBitis = donemBitis;

            string firmStr = firm.Value.ToString("D3");
            string periodStr = period.Value.ToString("D2");
            string firmaAdi = "";

            
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                SqlCommand firmaCmd = new SqlCommand("SELECT NAME FROM L_CAPIFIRM WHERE NR = @nr", con);
                firmaCmd.Parameters.AddWithValue("@nr", firm.Value);
                var firmaResult = firmaCmd.ExecuteScalar();
                firmaAdi = Convert.ToString(firmaResult) ?? "";
            }


            ViewBag.BaslangicTarihi = baslangicTarihi;
            ViewBag.BitisTarihi = bitisTarihi;
            ViewBag.HesapKoduBaslangic = hesapKoduBaslangic;
            ViewBag.HesapKoduBitis = hesapKoduBitis;
            ViewBag.HesapSeviyesi = hesapSeviyesi;
            ViewBag.HesapTuru = hesapTuru;
            ViewBag.HareketGormeyenler = hareketGormeyenler;
            ViewBag.BakiyeVermeyenler = bakiyeVermeyenler;
            ViewBag.KapanisFisi = kapanisFisi;
            ViewBag.Firma = firmStr + " - " + firmaAdi;


            var filtre = new MizanFiltreModel
            {
                BaslangicTarihi = baslangicTarihi,
                BitisTarihi = bitisTarihi,
                HesapKoduBaslangic = hesapKoduBaslangic,
                HesapKoduBitis = hesapKoduBitis,
                HesapSeviyesi = hesapSeviyesi,
                HesapTuru = hesapTuru,
                HareketGormeyenler = hareketGormeyenler,
                BakiyeVermeyenler = bakiyeVermeyenler,
                KapanisFisi = kapanisFisi
            };

            // Filtreleri session'a kaydet
            HttpContext.Session.SetString("mizanFiltre_baslangic", baslangicTarihi ?? "");
            HttpContext.Session.SetString("mizanFiltre_bitis", bitisTarihi ?? "");
            HttpContext.Session.SetString("mizanFiltre_kapanisFisi", kapanisFisi ?? "Dahil");

            decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;

            var model = MizanVerisiGetir(
                filtre,
                out toplamBorc,
                out toplamAlacak,
                out toplamBorcBakiye,
                out toplamAlacakBakiye);

            ViewBag.ToplamBorc = toplamBorc;
            ViewBag.ToplamAlacak = toplamAlacak;
            ViewBag.ToplamBorcBakiye = toplamBorcBakiye;
            ViewBag.ToplamAlacakBakiye = toplamAlacakBakiye;



            return View(model);


        }



        /*------------------------------------------------------Mizan Excel---------------------------------------------------*/

        [HttpGet]
        public IActionResult MizanExcel(
    string? baslangicTarihi,
    string? bitisTarihi,
    string? hesapKoduBaslangic,
    string? hesapKoduBitis,
    string? hesapSeviyesi,
    string? hesapTuru,
    string? hareketGormeyenler,
    string? bakiyeVermeyenler,
    string? kapanisFisi)
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB bağlantısı bulunamadı.");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (firm == null || period == null)
                return Content("Firma / dönem seçimi yapılmamış.");

            int firmNo = firm ?? 0;
            int periodNo = period ?? 0;

            string firmStr = firmNo.ToString("D3");
            string periodStr = periodNo.ToString("D2");
            string firmaAdi = "";

            var connStr = HttpContext.Session.GetString("db");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                SqlCommand firmaCmd = new SqlCommand("SELECT NAME FROM L_CAPIFIRM WHERE NR = @nr", con);
                firmaCmd.Parameters.AddWithValue("@nr", firmNo);

                var firmaResult = firmaCmd.ExecuteScalar();
                firmaAdi = Convert.ToString(firmaResult) ?? "";
            }

            var filtre = new MizanFiltreModel
            {
                BaslangicTarihi = baslangicTarihi,
                BitisTarihi = bitisTarihi,
                HesapKoduBaslangic = hesapKoduBaslangic,
                HesapKoduBitis = hesapKoduBitis,
                HesapSeviyesi = hesapSeviyesi,
                HesapTuru = hesapTuru,
                HareketGormeyenler = hareketGormeyenler,
                BakiyeVermeyenler = bakiyeVermeyenler,
                KapanisFisi = kapanisFisi
            };

            decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;

            var model = MizanVerisiGetir(
                filtre,
                out toplamBorc,
                out toplamAlacak,
                out toplamBorcBakiye,
                out toplamAlacakBakiye);

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Mizan");

                ws.Range("A1:F1").Merge();
                ws.Cell("A1").Value = "İKİ TARİH ARASI MİZAN";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 16;
                ws.Cell("A1").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                ws.Cell("A1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1F7A5A");
                ws.Cell("A1").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                ws.Cell("A2").Value = "Firma";
                ws.Cell("B2").Value = firmStr + " - " + firmaAdi;
                ws.Cell("A3").Value = "Başlangıç Tarihi";
                ws.Cell("B3").Value = baslangicTarihi;
                ws.Cell("C3").Value = "Bitiş Tarihi";
                ws.Cell("D3").Value = bitisTarihi;

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

                int row = 6;

                foreach (var item in model)
                {
                    ws.Cell(row, 1).Value = item.HesapKodu;
                    ws.Cell(row, 2).Value = item.HesapAdi;
                    ws.Cell(row, 3).Value = item.Borc;
                    ws.Cell(row, 4).Value = item.Alacak;
                    ws.Cell(row, 5).Value = item.BorcBakiye;
                    ws.Cell(row, 6).Value = item.AlacakBakiye;

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
                toplamRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                toplamRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#176347");
                toplamRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                toplamRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                ws.Range(6, 3, row, 6).Style.NumberFormat.Format = "#,##0.00";
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

        

        /*-----Mizan Verisi Getirme (Raporlama ve Excel için ortak)---------*/
        private List<MizanItem> MizanVerisiGetir(
    MizanFiltreModel filtre,
    out decimal toplamBorc,
    out decimal toplamAlacak,
    out decimal toplamBorcBakiye,
    out decimal toplamAlacakBakiye)
        {

            toplamBorc = 0;
            toplamAlacak = 0;
            toplamBorcBakiye = 0;
            toplamAlacakBakiye = 0;

            List<MizanItem> model = new List<MizanItem>();
            Dictionary<string, (decimal Borc, decimal Alacak)> hareketToplamlari = new();
            List<string> tumHesapKodlari = new List<string>();

            var connStr = HttpContext.Session.GetString("db");
            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (string.IsNullOrEmpty(connStr) || firm == null || period == null)
                return model;

            string firmStr = firm.Value.ToString("D3");
            string periodStr = period.Value.ToString("D2");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string hareketSql = $@"
    SELECT L.ACCOUNTCODE, SUM(L.DEBIT) AS TOPLAM_BORC, SUM(L.CREDIT) AS TOPLAM_ALACAK
    FROM LG_{firmStr}_{periodStr}_EMFLINE L
    INNER JOIN LG_{firmStr}_{periodStr}_EMFICHE F ON F.LOGICALREF = L.ACCFICHEREF
    WHERE (@baslangicTarihi IS NULL OR L.DATE_ >= @baslangicTarihi)
      AND (@bitisTarihi IS NULL OR L.DATE_ <= @bitisTarihi)
      AND L.CANCELLED = 0
      AND (@kapanisFisi IS NULL OR @kapanisFisi = 'Dahil' OR F.TRCODE <> 7)
    GROUP BY L.ACCOUNTCODE
";


                SqlCommand hareketCmd = new SqlCommand(hareketSql, con);
                hareketCmd.Parameters.AddWithValue("@baslangicTarihi",
                    string.IsNullOrEmpty(filtre.BaslangicTarihi) ? DBNull.Value : Convert.ToDateTime(filtre.BaslangicTarihi));
                hareketCmd.Parameters.AddWithValue("@bitisTarihi",
                    string.IsNullOrEmpty(filtre.BitisTarihi) ? DBNull.Value : Convert.ToDateTime(filtre.BitisTarihi));
                hareketCmd.Parameters.AddWithValue("@kapanisFisi", (object?)filtre.KapanisFisi ?? DBNull.Value);

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
                cmd.Parameters.AddWithValue("@hesapKoduBaslangic", (object?)filtre.HesapKoduBaslangic ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hesapKoduBitis", (object?)filtre.HesapKoduBitis ?? DBNull.Value);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string hesapKodu = dr["CODE"]?.ToString() ?? "";
                    string hesapAdi = dr["DEFINITION_"]?.ToString() ?? "";

                    tumHesapKodlari.Add(hesapKodu);

                    if (!string.IsNullOrEmpty(filtre.HesapSeviyesi))
                    {
                        int secilenSeviye = Convert.ToInt32(filtre.HesapSeviyesi);
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
                        borcBakiye = borc - alacak;
                    else if (alacak > borc)
                        alacakBakiye = alacak - borc;

                    if (!string.IsNullOrEmpty(filtre.HesapTuru) && filtre.HesapTuru != "Tumu")
                    {
                        bool uygunMu = false;

                        if (filtre.HesapTuru == "KdvHesaplari")
                        {
                            uygunMu = hesapKodu.StartsWith("190") ||
                                      hesapKodu.StartsWith("191") ||
                                      hesapKodu.StartsWith("391");
                        }
                        else if (filtre.HesapTuru == "Kkeg")
                        {
                            uygunMu = hesapKodu.StartsWith("689");
                        }
                        else if (filtre.HesapTuru == "GelirTablosu")
                        {
                            uygunMu = hesapKodu.StartsWith("600") ||
                                      hesapKodu.StartsWith("7");
                        }

                        if (!uygunMu)
                            continue;
                    }

                    if (filtre.HareketGormeyenler == "Listelenmeyecek" && borc == 0 && alacak == 0)
                        continue;

                    if (filtre.BakiyeVermeyenler == "Listelenmeyecek" && borcBakiye == 0 && alacakBakiye == 0)
                        continue;

                    model.Add(new MizanItem
                    {
                        HesapKodu = hesapKodu,
                        HesapAdi = hesapAdi,
                        Borc = borc,
                        Alacak = alacak,
                        BorcBakiye = borcBakiye,
                        AlacakBakiye = alacakBakiye,
                        Seviye = HesapSeviyesiBul(hesapKodu)
                    });
                }

                dr.Close();
            }

            foreach (var item in model)
            {
                if (!item.HesapKodu.Contains("."))
                {
                    toplamBorc += item.Borc;
                    toplamAlacak += item.Alacak;
                    toplamBorcBakiye += item.BorcBakiye;
                    toplamAlacakBakiye += item.AlacakBakiye;
                }
            }

            return model;
        }



        // ---------------- DASHBOARD ----------------
        public IActionResult Dashboard()
        {
            var user = HttpContext.Session.GetString("user");

            if (string.IsNullOrEmpty(user))
                return RedirectToAction("Login", "Account");

            var db = HttpContext.Session.GetString("db");
            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");
            var role = HttpContext.Session.GetString("role");

            ViewBag.DbConnected = !string.IsNullOrEmpty(db);
            ViewBag.CanUseReports = !string.IsNullOrEmpty(db) && firm.HasValue && period.HasValue;
            ViewBag.Role = role;

            if (role != "Admin")
            {
                var roleService = HttpContext.RequestServices.GetService<RoleService>();
                var roleObj = roleService?.GetRole(role ?? "");
                ViewBag.Permissions = roleObj?.Permissions ?? new List<string>();
            }
            else
            {
                ViewBag.Permissions = null;
            }

            // Dashboard kartları için veri
            if (!string.IsNullOrEmpty(db) && firm.HasValue && period.HasValue)
            {
                try
                {
                    var filtre = new MizanFiltreModel
                    {
                        HesapSeviyesi = "9",
                        KapanisFisi = "Dahil"
                    };

                    decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;
                    var mizanVerisi = MizanVerisiGetir(filtre, out toplamBorc, out toplamAlacak, out toplamBorcBakiye, out toplamAlacakBakiye);

                    // Net Satışlar
                    var netSatislar = mizanVerisi
                        .Where(x => !x.HesapKodu.Contains(".") &&
                            (x.HesapKodu.StartsWith("600") || x.HesapKodu.StartsWith("601") || x.HesapKodu.StartsWith("602")))
                        .Sum(x => x.Alacak);

                    // Toplam Maliyetler
                    var toplamMaliyet = mizanVerisi
                        .Where(x => !x.HesapKodu.Contains(".") &&
                            (x.HesapKodu.StartsWith("620") || x.HesapKodu.StartsWith("621") || x.HesapKodu.StartsWith("622")))
                        .Sum(x => x.Borc);

                    // Dönem Net Karı
                    var gelirler = netSatislar;
                    var giderler = toplamMaliyet;
                    var donemKar = gelirler - giderler;

                    ViewBag.NetSatislar = netSatislar.ToString("N2");
                    ViewBag.ToplamMaliyet = toplamMaliyet.ToString("N2");
                    ViewBag.DonemKar = donemKar.ToString("N2");
                }
                catch
                {
                    ViewBag.NetSatislar = "-";
                    ViewBag.ToplamMaliyet = "-";
                    ViewBag.DonemKar = "-";
                }
            }
            else
            {
                ViewBag.NetSatislar = "-";
                ViewBag.ToplamMaliyet = "-";
                ViewBag.DonemKar = "-";
            }

            return View();
        }
        /*-------IConfiguration------*/
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        //---------DATABASE SETTINGS------------

        [HttpGet]
        public IActionResult DbSettings()
        {
           
            // appsettings.json'daki connection string'i parse edip forma doldur
            var connStr = _configuration.GetConnectionString("DefaultConnection") ?? "";

            var model = new DbSetting();

            if (!string.IsNullOrEmpty(connStr))
            {
                var builder = new SqlConnectionStringBuilder(connStr);
                model.Server = builder.DataSource;
                model.Database = builder.InitialCatalog;
                model.Username = builder.UserID;
                model.Password = ""; // güvenlik için şifreyi forma doldurmuyoruz
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
        /*-----------------Mizan Ters Bakiye Kontrol-----------------*/
        [HttpGet]
        public JsonResult MizanTersBakiyeKontrol(
    string? baslangicTarihi,
    string? bitisTarihi,
    string? hesapKoduBaslangic,
    string? hesapKoduBitis,
    string? hesapSeviyesi,
    string? hesapTuru,
    string? hareketGormeyenler,
    string? bakiyeVermeyenler,
    string? kapanisFisi)
        {
            var filtre = new MizanFiltreModel
            {
                BaslangicTarihi = baslangicTarihi,
                BitisTarihi = bitisTarihi,
                HesapKoduBaslangic = hesapKoduBaslangic,
                HesapKoduBitis = hesapKoduBitis,
                HesapSeviyesi = hesapSeviyesi,
                HesapTuru = hesapTuru,
                HareketGormeyenler = hareketGormeyenler,
                BakiyeVermeyenler = bakiyeVermeyenler,
                KapanisFisi = kapanisFisi
            };

            decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;

            var model = MizanVerisiGetir(
                filtre,
                out toplamBorc,
                out toplamAlacak,
                out toplamBorcBakiye,
                out toplamAlacakBakiye);

            var uyarilar = new List<string>();

            foreach (var item in model)
            {
                string kod = item.HesapKodu;
                
                if (item.Borc == 0 && item.Alacak == 0 && item.BorcBakiye == 0 && item.AlacakBakiye == 0)
                    continue;

                if (kod.StartsWith("1") && item.AlacakBakiye > 0.01m)
                {
                    uyarilar.Add($"{kod} - {item.HesapAdi}: Aktif hesapta alacak bakiye ({item.AlacakBakiye:N2})");
                }
                else if ((kod.StartsWith("3") || kod.StartsWith("4")) && item.BorcBakiye > 0.01m)
                {
                    uyarilar.Add($"{kod} - {item.HesapAdi}: Pasif hesapta borç bakiye ({item.BorcBakiye:N2})");
                }
                else if (kod.StartsWith("7") && item.AlacakBakiye > 0.01m)
                {
                    uyarilar.Add($"{kod} - {item.HesapAdi}: Gider hesabında alacak bakiye ({item.AlacakBakiye:N2})");
                }
            }


            return Json(new
            {
                uyariSayisi = uyarilar.Count,
                uyarilar = uyarilar
            });
        }

        /*-----------------Kar/Zarar Tablosu-----------------*/

        [HttpGet]
public IActionResult KarZarar()
{
    if (HttpContext.Session.GetString("db") == null)
        return Content("DB bağlantısı bulunamadı.");

    var firm = HttpContext.Session.GetInt32("firm");
    var period = HttpContext.Session.GetInt32("period");

    if (firm == null || period == null)
        return Content("Firma / dönem seçimi yapılmamış.");

    var baslangic = HttpContext.Session.GetString("mizanFiltre_baslangic");
    var bitis = HttpContext.Session.GetString("mizanFiltre_bitis");
    var kapanisFisi = HttpContext.Session.GetString("mizanFiltre_kapanisFisi");

    var filtre = new MizanFiltreModel
    {
        BaslangicTarihi = string.IsNullOrEmpty(baslangic) ? null : baslangic,
        BitisTarihi = string.IsNullOrEmpty(bitis) ? null : bitis,
        HesapSeviyesi = "9",
        HareketGormeyenler = "Listelenmeyecek",
        BakiyeVermeyenler = "Listelenmeyecek",
        KapanisFisi = string.IsNullOrEmpty(kapanisFisi) ? "Dahil" : kapanisFisi
    };

    decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;
    var mizan = MizanVerisiGetir(filtre, out toplamBorc, out toplamAlacak, out toplamBorcBakiye, out toplamAlacakBakiye);

    var model = new List<GelirTablosuSonucItem>();

    // Yardımcı metodlar
    void SatirEkle(string aciklama, decimal tutar, bool kalin, bool gider = false)
    {
        model.Add(new GelirTablosuSonucItem
        {
            Aciklama = aciklama,
            Tutar = gider ? -tutar : tutar,
            KalinMi = kalin
        });
    }

    void HesapGrubuEkle(string[] kodlar, bool gider = false)
    {
        var hesaplar = mizan.Where(x => !x.HesapKodu.Contains(".") &&
            kodlar.Any(k => x.HesapKodu.StartsWith(k)))
            .OrderBy(x => x.HesapKodu);

        foreach (var h in hesaplar)
        {
            var tutar = gider ? h.Borc : h.Alacak;
            if (tutar == 0) continue;
            model.Add(new GelirTablosuSonucItem
            {
                Aciklama = $"  {h.HesapKodu} - {h.HesapAdi}",
                Tutar = gider ? -tutar : tutar,
                KalinMi = false
            });
        }
    }

    // 1. SATIŞLAR
    SatirEkle("A - BRÜT SATIŞLAR", 0, true);
    HesapGrubuEkle(new[] { "600", "601", "602" });
    var satislar = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("600") || x.HesapKodu.StartsWith("601") || x.HesapKodu.StartsWith("602")))
        .Sum(x => x.Alacak);
    SatirEkle("Brüt Satışlar Toplamı", satislar, true);

    // 2. SATIŞ İNDİRİMLERİ
    SatirEkle("B - SATIŞ İNDİRİMLERİ (-)", 0, true);
    HesapGrubuEkle(new[] { "610", "611", "612" }, true);
    var indirimler = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("610") || x.HesapKodu.StartsWith("611") || x.HesapKodu.StartsWith("612")))
        .Sum(x => x.Alacak);
    SatirEkle("Satış İndirimleri Toplamı", -indirimler, true);

    var netSatislar = satislar - indirimler;
    SatirEkle("NET SATIŞLAR", netSatislar, true);

    // 3. SATIŞ MALİYETİ
    SatirEkle("C - SATIŞLARIN MALİYETİ (-)", 0, true);
    HesapGrubuEkle(new[] { "620", "621", "622", "623" }, true);
    var satisMaliyeti = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("620") || x.HesapKodu.StartsWith("621") ||
         x.HesapKodu.StartsWith("622") || x.HesapKodu.StartsWith("623")))
        .Sum(x => x.Borc);
    SatirEkle("Satış Maliyeti Toplamı", -satisMaliyeti, true);

    decimal brutKar = netSatislar - satisMaliyeti;
    SatirEkle("BRÜT SATIŞ KARI / ZARARI", brutKar, true);

    // 4. FAALİYET GİDERLERİ
    SatirEkle("D - FAALİYET GİDERLERİ (-)", 0, true);
    HesapGrubuEkle(new[] { "630", "631", "632", "653", "654", "655", "656", "657", "658", "659" }, true);
    var faaliyetGiderleri = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("630") || x.HesapKodu.StartsWith("631") ||
         x.HesapKodu.StartsWith("632") || x.HesapKodu.StartsWith("653") ||
         x.HesapKodu.StartsWith("654") || x.HesapKodu.StartsWith("655") ||
         x.HesapKodu.StartsWith("656") || x.HesapKodu.StartsWith("657") ||
         x.HesapKodu.StartsWith("658") || x.HesapKodu.StartsWith("659")))
        .Sum(x => x.Borc);
    SatirEkle("Faaliyet Giderleri Toplamı", -faaliyetGiderleri, true);

    decimal faaliyetKar = brutKar - faaliyetGiderleri;
    SatirEkle("FAALİYET KARI / ZARARI", faaliyetKar, true);

    // 5. DİĞER GELİRLER
    SatirEkle("E - DİĞER OLAĞAN GELİR VE KARLAR (+)", 0, true);
    HesapGrubuEkle(new[] { "641", "642", "643", "644", "645", "646", "647", "648", "649", "671", "679" });
    var digerGelirler = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("64") || x.HesapKodu.StartsWith("671") || x.HesapKodu.StartsWith("679")))
        .Sum(x => x.Alacak);
    SatirEkle("Diğer Gelirler Toplamı", digerGelirler, true);

    // 6. FİNANSMAN GİDERLERİ
    SatirEkle("F - FİNANSMAN GİDERLERİ (-)", 0, true);
    HesapGrubuEkle(new[] { "660", "661" }, true);
    var finansmanGiderleri = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("660") || x.HesapKodu.StartsWith("661")))
        .Sum(x => x.Borc);
    SatirEkle("Finansman Giderleri Toplamı", -finansmanGiderleri, true);

    decimal olaganKar = faaliyetKar + digerGelirler - finansmanGiderleri;
    SatirEkle("OLAĞAN KARI / ZARARI", olaganKar, true);

    // 7. DİĞER GİDERLER
    SatirEkle("G - OLAĞANDIŞI GELİR VE KARLAR (+)", 0, true);
    HesapGrubuEkle(new[] { "671", "679" });
    SatirEkle("H - OLAĞANDIŞI GİDER VE ZARARLAR (-)", 0, true);
    HesapGrubuEkle(new[] { "680", "681", "689" }, true);
    var digerGiderler = mizan.Where(x => !x.HesapKodu.Contains(".") &&
        (x.HesapKodu.StartsWith("680") || x.HesapKodu.StartsWith("681") ||
         x.HesapKodu.StartsWith("689")))
        .Sum(x => x.Borc);
    SatirEkle("Olağandışı Giderler Toplamı", -digerGiderler, true);

    decimal donemKar = olaganKar - digerGiderler;
    SatirEkle("DÖNEM KARI / ZARARI", donemKar, true);

    decimal vergiKarsiligi = donemKar > 0 ? donemKar * 0.25m : 0;
    SatirEkle("Vergi Karşılığı (-)", -vergiKarsiligi, false);

    decimal donemNetKar = donemKar - vergiKarsiligi;
    SatirEkle("DÖNEM NET KARI / ZARARI", donemNetKar, true);

    ViewBag.BaslangicTarihi = baslangic;
    ViewBag.BitisTarihi = bitis;
    ViewBag.Firma = firm.Value.ToString("D3");
    ViewBag.NetSatislar = netSatislar;
    ViewBag.BrutKar = brutKar;
    ViewBag.FaaliyetKar = faaliyetKar;
    ViewBag.OlaganKar = olaganKar;
    ViewBag.DonemKar = donemKar;
    ViewBag.DonemNetKar = donemNetKar;

    return View(model);
}
        /*-----------------Gelir Tablosu Analizi-----------------*/

        [HttpGet]
        public IActionResult GelirTablosuAnalizi()
        {
            if (HttpContext.Session.GetString("db") == null)
                return Content("DB bağlantısı bulunamadı.");

            var firm = HttpContext.Session.GetInt32("firm");
            var period = HttpContext.Session.GetInt32("period");

            if (firm == null || period == null)
                return Content("Firma / dönem seçimi yapılmamış.");

            var baslangic = HttpContext.Session.GetString("mizanFiltre_baslangic");
            var bitis = HttpContext.Session.GetString("mizanFiltre_bitis");
            var kapanisFisi = HttpContext.Session.GetString("mizanFiltre_kapanisFisi");

            var filtre = new MizanFiltreModel
            {
                BaslangicTarihi = string.IsNullOrEmpty(baslangic) ? null : baslangic,
                BitisTarihi = string.IsNullOrEmpty(bitis) ? null : bitis,
                HesapSeviyesi = "9",
                HareketGormeyenler = "Listelenecek",
                BakiyeVermeyenler = "Listelenecek",
                KapanisFisi = string.IsNullOrEmpty(kapanisFisi) ? "Dahil" : kapanisFisi
            };


            decimal toplamBorc, toplamAlacak, toplamBorcBakiye, toplamAlacakBakiye;

            var mizanVerisi = MizanVerisiGetir(
                filtre,
                out toplamBorc,
                out toplamAlacak,
                out toplamBorcBakiye,
                out toplamAlacakBakiye);

            List<GelirTablosuItem> gelirler = new List<GelirTablosuItem>();

            // 600-601-602
            var satisHesaplari = mizanVerisi
    .Where(x => !x.HesapKodu.Contains(".") &&
                (x.HesapKodu.StartsWith("600") || x.HesapKodu.StartsWith("601") || x.HesapKodu.StartsWith("602")))
    .OrderBy(x => x.HesapKodu)
    .ToList();


            gelirler.AddRange(satisHesaplari.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Alacak,
                KalinMi = false
            }));

            decimal satisToplami = satisHesaplari.Sum(x => x.Alacak);

            gelirler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "SATIŞLAR TOPLAMI",
                Alacak = satisToplami,
                KalinMi = true
            });

            // 610-611-612
            var indirimHesaplari = mizanVerisi
    .Where(x => !x.HesapKodu.Contains(".") &&
                (x.HesapKodu.StartsWith("610") || x.HesapKodu.StartsWith("611") || x.HesapKodu.StartsWith("612")))
    .OrderBy(x => x.HesapKodu)
    .ToList();


            gelirler.AddRange(indirimHesaplari.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Alacak,
                KalinMi = false
            }));

            decimal indirimToplami = indirimHesaplari.Sum(x => x.Alacak);

            gelirler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "İNDİRİMLER TOPLAMI",
                Alacak = indirimToplami,
                KalinMi = true
            });

            decimal netSatislarToplami = satisToplami - indirimToplami;

            gelirler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "NET SATIŞLAR TOPLAMI",
                Alacak = netSatislarToplami,
                KalinMi = true
            });

            // Diğer gelirler
            var digerGelirKodlari = new[] { "641", "642", "643", "644", "645", "646", "647", "648", "649", "671", "679" };

            var digerGelirler = mizanVerisi
    .Where(x => !x.HesapKodu.Contains(".") &&
                digerGelirKodlari.Any(k => x.HesapKodu.StartsWith(k)))
    .OrderBy(x => x.HesapKodu)
    .ToList();


            gelirler.AddRange(digerGelirler.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Alacak,
                KalinMi = false
            }));

            decimal digerGelirlerToplami = digerGelirler.Sum(x => x.Alacak);

            gelirler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "DİĞER GELİRLER TOPLAMI",
                Alacak = digerGelirlerToplami,
                KalinMi = true
            });

            decimal gelirlerToplami = netSatislarToplami + digerGelirlerToplami;

            gelirler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "GELİRLER TOPLAMI",
                Alacak = gelirlerToplami,
                KalinMi = true
            });
            List<GelirTablosuItem> giderler = new List<GelirTablosuItem>();

            // 620-621-622-623
            var satisMaliyetiKodlari = new[] { "620", "621", "622", "623" };

            var satisMaliyeti = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            satisMaliyetiKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            giderler.AddRange(satisMaliyeti.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Borc,
                KalinMi = false
            }));

            decimal satisMaliyetiToplami = satisMaliyeti.Sum(x => x.Borc);

            giderler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "SATIŞ MALİYETİ",
                Alacak = satisMaliyetiToplami,
                KalinMi = true
            });

            // 630-631-632-653-654-655-656-657-658-659
            var faaliyetGiderKodlari = new[] { "630", "631", "632", "653", "654", "655", "656", "657", "658", "659" };

            var faaliyetGiderleri = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            faaliyetGiderKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            giderler.AddRange(faaliyetGiderleri.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Borc,
                KalinMi = false
            }));

            decimal faaliyetGiderToplami = faaliyetGiderleri.Sum(x => x.Borc);

            giderler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "FAALİYET GİDERLERİ",
                Alacak = faaliyetGiderToplami,
                KalinMi = true
            });

            // 660-661
            var finansmanGiderKodlari = new[] { "660", "661" };

            var finansmanGiderleri = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            finansmanGiderKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            giderler.AddRange(finansmanGiderleri.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Borc,
                KalinMi = false
            }));

            decimal finansmanGiderToplami = finansmanGiderleri.Sum(x => x.Borc);

            giderler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "FİNANSMAN GİDERLERİ",
                Alacak = finansmanGiderToplami,
                KalinMi = true
            });

            // 680-681-689
            var digerGiderKodlari = new[] { "680", "681", "689" };

            var digerGiderler = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            digerGiderKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            giderler.AddRange(digerGiderler.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.Borc,
                KalinMi = false
            }));

            decimal digerGiderToplami = digerGiderler.Sum(x => x.Borc);

            giderler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "DİĞER GİDERLER",
                Alacak = digerGiderToplami,
                KalinMi = true
            });

            decimal giderlerToplami = satisMaliyetiToplami + faaliyetGiderToplami + finansmanGiderToplami + digerGiderToplami;

            giderler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "GİDERLER TOPLAMI",
                Alacak = giderlerToplami,
                KalinMi = true
            });

            ViewBag.Giderler = giderler;

            List<GelirTablosuItem> maliyetiEtkileyenler = new List<GelirTablosuItem>();

            // 150,151,152,153,157
            var stokKodlari = new[] { "150", "151", "152", "153", "157" };

            var stokHesaplari = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            stokKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(stokHesaplari.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal stokToplami = stokHesaplari.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "STOKLARIN TOPLAMI",
                Alacak = stokToplami,
                KalinMi = true
            });

            // 159
            var siparisAvanslari = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") && x.HesapKodu.StartsWith("159"))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(siparisAvanslari.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal siparisAvanslariToplami = siparisAvanslari.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "VERİLEN SİPARİŞ AVANSLARI",
                Alacak = siparisAvanslariToplami,
                KalinMi = true
            });

            // 180
            var gelecekAylaraAitGiderler = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") && x.HesapKodu.StartsWith("180"))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(gelecekAylaraAitGiderler.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal gelecekAylaraAitGiderlerToplami = gelecekAylaraAitGiderler.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "GELECEK AYLARA AİT GİDERLER",
                Alacak = gelecekAylaraAitGiderlerToplami,
                KalinMi = true
            });

            // 710,720,730
            var uretimGiderKodlari = new[] { "710", "720", "730" };

            var uretimGiderleri = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            uretimGiderKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(uretimGiderleri.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal uretimGiderleriToplami = uretimGiderleri.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "ÜRETİM GİDERLERİ",
                Alacak = uretimGiderleriToplami,
                KalinMi = true
            });

            // 740
            var hizmetUretim = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") && x.HesapKodu.StartsWith("740"))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(hizmetUretim.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal hizmetUretimToplami = hizmetUretim.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "HİZMET ÜRETİM MALİYETİ",
                Alacak = hizmetUretimToplami,
                KalinMi = true
            });

            // 750,760,770,780
            var yonetimMaliyetKodlari = new[] { "750", "760", "770", "780" };

            var yonetimMaliyetleri = mizanVerisi
                .Where(x => !x.HesapKodu.Contains(".") &&
                            yonetimMaliyetKodlari.Any(k => x.HesapKodu.StartsWith(k)))
                .OrderBy(x => x.HesapKodu)
                .ToList();

            maliyetiEtkileyenler.AddRange(yonetimMaliyetleri.Select(x => new GelirTablosuItem
            {
                HesapKodu = x.HesapKodu,
                HesapAdi = x.HesapAdi,
                Alacak = x.BorcBakiye,
                KalinMi = false
            }));

            decimal yonetimMaliyetleriToplami = yonetimMaliyetleri.Sum(x => x.BorcBakiye);

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "YÖNETİM MALİYETLERİ",
                Alacak = yonetimMaliyetleriToplami,
                KalinMi = true
            });

            // Genel toplam
            decimal olasiGiderlerToplami =
                stokToplami +
                siparisAvanslariToplami +
                gelecekAylaraAitGiderlerToplami +
                uretimGiderleriToplami +
                hizmetUretimToplami +
                yonetimMaliyetleriToplami;

            maliyetiEtkileyenler.Add(new GelirTablosuItem
            {
                HesapKodu = "",
                HesapAdi = "OLASI GİDERLER TOPLAMI",
                Alacak = olasiGiderlerToplami,
                KalinMi = true
            });

            ViewBag.MaliyetiEtkileyenler = maliyetiEtkileyenler;

            List<GelirTablosuSonucItem> sonucPaneli = new List<GelirTablosuSonucItem>();

            decimal brutSatisKarZarar = gelirlerToplami - satisMaliyetiToplami;
            decimal faaliyetKarZarar = brutSatisKarZarar - faaliyetGiderToplami;
            decimal olaganKarZarar = faaliyetKarZarar - finansmanGiderToplami;
            decimal donemKarZarar = olaganKarZarar - digerGiderToplami;

            // Şimdilik vergi karşılığı 0 bırakıyoruz
            decimal vergiKarsiligi = donemKarZarar*0.25m;
            decimal donemNetKarZarar = donemKarZarar - vergiKarsiligi;

            string Oran(decimal tutar)
            {
                if (netSatislarToplami == 0)
                    return "-";

                return ((tutar / netSatislarToplami) * 100).ToString("N2") + " %";
            }

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Net Satışların Toplamı",
                Tutar = gelirlerToplami,
                KarlilikOrani = Oran(gelirlerToplami),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Satışların Maliyeti",
                Tutar = satisMaliyetiToplami,
                KarlilikOrani = Oran(satisMaliyetiToplami),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "BRÜT SATIŞ KARI VEYA ZARARI",
                Tutar = brutSatisKarZarar,
                KarlilikOrani = Oran(brutSatisKarZarar),
                KalinMi = true
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Faaliyet Giderleri",
                Tutar = faaliyetGiderToplami,
                KarlilikOrani = Oran(faaliyetGiderToplami),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "FAALİYET KARI VEYA ZARARI",
                Tutar = faaliyetKarZarar,
                KarlilikOrani = Oran(faaliyetKarZarar),
                KalinMi = true
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Finansman Giderleri",
                Tutar = finansmanGiderToplami,
                KarlilikOrani = Oran(finansmanGiderToplami),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "OLAĞAN KARI VEYA ZARARI",
                Tutar = olaganKarZarar,
                KarlilikOrani = Oran(olaganKarZarar),
                KalinMi = true
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Olağandışı Gider ve Zararlar",
                Tutar = digerGiderToplami,
                KarlilikOrani = Oran(digerGiderToplami),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "DÖNEM KARI VEYA ZARARI",
                Tutar = donemKarZarar,
                KarlilikOrani = Oran(donemKarZarar),
                KalinMi = true
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "Vergi Karşılığı",
                Tutar = vergiKarsiligi,
                KarlilikOrani = Oran(vergiKarsiligi),
                KalinMi = false
            });

            sonucPaneli.Add(new GelirTablosuSonucItem
            {
                Aciklama = "DÖNEM NET KARI VEYA ZARARI",
                Tutar = donemNetKarZarar,
                KarlilikOrani = Oran(donemNetKarZarar),
                KalinMi = true
            });

            ViewBag.SonucPaneli = sonucPaneli;
            ViewBag.BrutKarZarar = brutSatisKarZarar.ToString("N2");
            ViewBag.FaaliyetKarZarar = faaliyetKarZarar.ToString("N2");
            ViewBag.OlaganKarZarar = olaganKarZarar.ToString("N2");
            ViewBag.DonemKarZarar = donemKarZarar.ToString("N2");
            ViewBag.DonemNetKarZarar = donemNetKarZarar.ToString("N2");


            return View(gelirler);
        }

            
    }
}