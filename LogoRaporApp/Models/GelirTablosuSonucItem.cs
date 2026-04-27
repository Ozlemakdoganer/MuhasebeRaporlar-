namespace LogoRaporApp.Models
{
    public class GelirTablosuSonucItem
    {
        public string Aciklama { get; set; } = "";
        public decimal Tutar { get; set; }
        public string KarlilikOrani { get; set; } = "";
        public bool KalinMi { get; set; }
    }
}
