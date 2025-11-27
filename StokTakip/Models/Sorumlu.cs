using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class Sorumlu
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Sorumlu adı boş bırakılamaz.")]
    [StringLength(100, ErrorMessage = "Sorumlu adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Sorumlu Adı")]
    public string SorumluAdi { get; set; } = null!;

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
