namespace LogoRaporApp.Models
{
    public class GelirTablosuItem
    {
        public string HesapKodu { get; set; } = "";
        public string HesapAdi { get; set; } = "";
        public decimal Alacak { get; set; }
        public bool KalinMi { get; set; }
    }
}
