using System.Collections.Generic;

// Projenizin namespace'ini kontrol edin (örn: MVC_stok_takip.Models)
namespace StokTakip.Models
{
    public class HomeViewModel
    {
        public int ToplamStokKartiSayisi { get; set; }
        public int ToplamAnaDepoSayisi { get; set; }
        public int ToplamKullaniciSayisi { get; set; }
        public List<StokHareket> SonStokHareketleri { get; set; }
        public decimal ToplamStokAdedi { get; internal set; }
        public List<UyariModel> Uyarilar { get; set; }
        public int OkunmamisUyariSayisi { get; set; }
        
        public HomeViewModel()
        {
            SonStokHareketleri = new List<StokHareket>();
            Uyarilar = new List<UyariModel>();
        }
    }
}