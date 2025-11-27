using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class DepoEslestirme
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Depo seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir depo seçiniz.")]
    [Display(Name = "Depo")]
    public int DepoId { get; set; }

    [Required(ErrorMessage = "Alt depo seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir alt depo seçiniz.")]
    [Display(Name = "Alt Depo")]
    public int AltDepoId { get; set; }

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

    public virtual AltDepo AltDepo { get; set; } = null!;

    public virtual Depo Depo { get; set; } = null!;

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }

    public virtual ICollection<StokDurum> StokDurums { get; set; } = new List<StokDurum>();

    public virtual ICollection<StokHareket> StokHarekets { get; set; } = new List<StokHareket>();
}
