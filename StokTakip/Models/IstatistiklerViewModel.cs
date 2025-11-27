namespace StokTakip.Models
{
    public class IstatistiklerViewModel
    {
        public int ToplamUrunCesidi { get; set; }
        public decimal ToplamStokAdedi { get; set; }
        public int ToplamDepoSayisi { get; set; }
        public int BuAykiHareketSayisi { get; set; }

        // ChartData modelini yeniden kullanarak hem isim hem de değer tutabiliriz.
        public ChartData EnCokBulunanUrun { get; set; } = null!;
        public ChartData EnAzBulunanUrun { get; set; } = null!;
        public ChartData EnAktifUrun { get; set; } = null!; // En çok hareketi olan ürün
    }
}