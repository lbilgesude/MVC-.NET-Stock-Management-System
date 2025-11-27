using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class StokDurum
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Stok seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir stok seçiniz.")]
    [Display(Name = "Stok")]
    public int StokId { get; set; }

    [Required(ErrorMessage = "Depo eşleştirmesi seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir depo eşleştirmesi seçiniz.")]
    [Display(Name = "Depo Eşleştirmesi")]
    public int DepoEslestirmeId { get; set; }

    [Required(ErrorMessage = "Durum miktarı boş bırakılamaz.")]
    [Range(0, double.MaxValue, ErrorMessage = "Durum miktarı 0'dan küçük olamaz.")]
    [Display(Name = "Durum Miktarı")]
    public decimal DurumMiktar { get; set; }

    [Display(Name = "Oluşturan Kullanıcı")]
    public int? OlusturanKullanici { get; set; }

    [Display(Name = "Güncelleyen Kullanıcı")]
    public int? GuncelleyenKullanici { get; set; }

    [Display(Name = "Oluşturma Tarihi")]
    [DataType(DataType.DateTime)]
    public DateTime? OlusturmaTarihi { get; set; }

    [Display(Name = "Güncelleme Tarihi")]
    [DataType(DataType.DateTime)]
    public DateTime? GuncellemeTarihi { get; set; }

    public virtual DepoEslestirme DepoEslestirme { get; set; } = null!;

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }

    public virtual Stok Stok { get; set; } = null!;
}