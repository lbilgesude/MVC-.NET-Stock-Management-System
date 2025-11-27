using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class HareketTip
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hareket tipi adı boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Hareket tipi adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Hareket Tipi Adı")]
    public string HareketTipAdi { get; set; } = null!;

    [Display(Name = "İşlem Göstergesi")]
    public bool IslemGostergesi { get; set; }

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

    public virtual ICollection<StokHareket> StokHarekets { get; set; } = new List<StokHareket>();
}
