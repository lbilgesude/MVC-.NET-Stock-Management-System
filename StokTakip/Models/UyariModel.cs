using System;

namespace StokTakip.Models
{
    public class UyariModel
    {
        public int Id { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public UyariTipi Tip { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public bool Okundu { get; set; }
        public string? AksiyonUrl { get; set; }
        public string? AksiyonText { get; set; }
    }

    public enum UyariTipi
    {
        Bilgi,
        Uyari,
        Hata,
        Basarili
    }
}
