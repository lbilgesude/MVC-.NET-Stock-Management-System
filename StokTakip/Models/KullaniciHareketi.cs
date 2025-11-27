using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models
{
    public class KullaniciHareketi
    {
        [Key]
        public int Id { get; set; }

        public int KullaniciId { get; set; } // Hareketi yapan kullanıcının ID'si
        public string HareketTip { get; set; } = string.Empty; // "Giriş", "Çıkış", "Kullanıcı Oluşturma" vb.
        public string Aciklama { get; set; } = string.Empty; // Hareketle ilgili detaylı bilgi
        public DateTime HareketTarihi { get; set; }

        [ForeignKey("KullaniciId")]
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}