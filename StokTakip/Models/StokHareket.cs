using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class StokHareket
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir ürün seçiniz.")]
    public int StokId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir depo seçiniz.")]
    public int DepoEslestirmeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir sorumlu seçiniz.")]
    public int SorumluId { get; set; }

    public int HareketTip { get; set; }

    public string? Aciklama { get; set; }

    [Required(ErrorMessage = "Miktar alanı boş bırakılamaz.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır.")]
    public decimal HareketMiktar { get; set; }

    public DateTime HareketTarihi { get; set; }

    public int? OlusturanKullanici { get; set; }

    public int? GuncelleyenKullanici { get; set; }

    public DateTime? OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public virtual DepoEslestirme? DepoEslestirme { get; set; }

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual HareketTip? HareketTipNavigation { get; set; }

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }

    public virtual Sorumlu? Sorumlu { get; set; }

    public virtual Stok? Stok { get; set; }
}