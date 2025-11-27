using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class Stok
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Stok adı boş bırakılamaz.")]
    [StringLength(100, ErrorMessage = "Stok adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Stok Adı")]
    public string StokAd { get; set; } = null!;

    [Required(ErrorMessage = "Ölçü birimi seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ölçü birimi seçiniz.")]
    [Display(Name = "Ölçü Birimi")]
    public int StokOlcuBirim { get; set; }

    [Required(ErrorMessage = "Stok markası boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Stok markası en fazla 50 karakter olabilir.")]
    [Display(Name = "Stok Markası")]
    public string StokMarka { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Stok detayı en fazla 500 karakter olabilir.")]
    [Display(Name = "Stok Detayı")]
    public string? StokDetay { get; set; }

    [Required(ErrorMessage = "Kayıt tarihi boş bırakılamaz.")]
    [DataType(DataType.Date)]
    [Display(Name = "Kayıt Tarihi")]
    public DateTime KayitTarihi { get; set; }

    [Required(ErrorMessage = "Kayıt miktarı boş bırakılamaz.")]
    [Range(0, double.MaxValue, ErrorMessage = "Kayıt miktarı 0'dan küçük olamaz.")]
    [Display(Name = "Kayıt Miktarı")]
    public decimal KayitMiktar { get; set; }

    [Display(Name = "Durum")]
    public bool Statu { get; set; }

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

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }

    public virtual ICollection<StokDurum> StokDurums { get; set; } = new List<StokDurum>();

    public virtual ICollection<StokHareket> StokHarekets { get; set; } = new List<StokHareket>();

    public virtual OlcuBirimi StokOlcuBirimNavigation { get; set; } = null!;
}