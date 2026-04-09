namespace LogoRaporApp.Models
{
    public class MizanItem
    {
        public string HesapKodu { get; set; } = "";
        public string HesapAdi { get; set; } = "";
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal BorcBakiye { get; set; }
        public decimal AlacakBakiye { get; set; }
           }
}
