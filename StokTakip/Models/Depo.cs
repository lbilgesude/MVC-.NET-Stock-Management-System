using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class Depo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Depo adı boş bırakılamaz.")]
    [StringLength(100, ErrorMessage = "Depo adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Depo Adı")]
    public string DepoAdi { get; set; } = null!;

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

    public virtual ICollection<DepoEslestirme> DepoEslestirmes { get; set; } = new List<DepoEslestirme>();

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }
}
