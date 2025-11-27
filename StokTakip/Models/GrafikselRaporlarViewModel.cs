using System.Collections.Generic;

namespace StokTakip.Models
{
    public class GrafikselRaporlarViewModel
    {
        public List<ChartData> AylikCikislar { get; set; }
        public List<ChartData> EnCokCikisiYapilanUrunler { get; set; }
        public List<ChartData> EnAzStoguKalanUrunler { get; set; }
        
        // YENİ STOK AKIŞ VERİLERİ
        public List<ChartData> AylikGirisler { get; set; }
        public List<ChartData> AylikTransferler { get; set; }
        public List<ChartData> DepoBazliStokDurumu { get; set; }
        public List<ChartData> HareketTipiDagilimi { get; set; }
        public List<ChartData> AltDepoStokDagilimi { get; set; }
        public List<ChartData> GunlukHareketTrendi { get; set; }
        public List<ChartData> KritikStokSeviyeleri { get; set; }
        public List<ChartData> EnAktifKullanicilar { get; set; }

        public GrafikselRaporlarViewModel()
        {
            AylikCikislar = new List<ChartData>();
            EnCokCikisiYapilanUrunler = new List<ChartData>();
            EnAzStoguKalanUrunler = new List<ChartData>();
            
            // YENİ STOK AKIŞ VERİLERİ
            AylikGirisler = new List<ChartData>();
            AylikTransferler = new List<ChartData>();
            DepoBazliStokDurumu = new List<ChartData>();
            HareketTipiDagilimi = new List<ChartData>();
            AltDepoStokDagilimi = new List<ChartData>();
            GunlukHareketTrendi = new List<ChartData>();
            KritikStokSeviyeleri = new List<ChartData>();
            EnAktifKullanicilar = new List<ChartData>();
        }
    }
}