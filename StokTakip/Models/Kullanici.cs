using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models;

public partial class Kullanici
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
    [Display(Name = "Kullanıcı Adı")]
    public string KulUsername { get; set; } = null!;

    [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6, en fazla 100 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string KulSifre { get; set; } = null!;

    [Required(ErrorMessage = "Ad alanı boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string KulAd { get; set; } = null!;

    [Required(ErrorMessage = "Soyad alanı boş bırakılamaz.")]
    [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string KulSoyad { get; set; } = null!;

    [Required(ErrorMessage = "Kullanıcı tipi seçilmelidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kullanıcı tipi seçiniz.")]
    [Display(Name = "Kullanıcı Tipi")]
    public int KulTip { get; set; }

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

    public virtual ICollection<AltDepo> AltDepoGuncelleyenKullaniciNavigations { get; set; } = new List<AltDepo>();

    public virtual ICollection<AltDepo> AltDepoOlusturanKullaniciNavigations { get; set; } = new List<AltDepo>();

    public virtual ICollection<DepoEslestirme> DepoEslestirmeGuncelleyenKullaniciNavigations { get; set; } = new List<DepoEslestirme>();

    public virtual ICollection<DepoEslestirme> DepoEslestirmeOlusturanKullaniciNavigations { get; set; } = new List<DepoEslestirme>();

    public virtual ICollection<Depo> DepoGuncelleyenKullaniciNavigations { get; set; } = new List<Depo>();

    public virtual ICollection<Depo> DepoOlusturanKullaniciNavigations { get; set; } = new List<Depo>();

    public virtual Kullanici? GuncelleyenKullaniciNavigation { get; set; }

    public virtual ICollection<HareketTip> HareketTipGuncelleyenKullaniciNavigations { get; set; } = new List<HareketTip>();

    public virtual ICollection<HareketTip> HareketTipOlusturanKullaniciNavigations { get; set; } = new List<HareketTip>();

    public virtual ICollection<Kullanici> InverseGuncelleyenKullaniciNavigation { get; set; } = new List<Kullanici>();

    public virtual ICollection<Kullanici> InverseOlusturanKullaniciNavigation { get; set; } = new List<Kullanici>();

    public virtual KullaniciTip KulTipNavigation { get; set; } = null!;

    public virtual ICollection<KullaniciTip> KullaniciTipGuncelleyenKullaniciNavigations { get; set; } = new List<KullaniciTip>();

    public virtual ICollection<KullaniciTip> KullaniciTipOlusturanKullaniciNavigations { get; set; } = new List<KullaniciTip>();

    public virtual ICollection<OlcuBirimi> OlcuBirimiGuncelleyenKullaniciNavigations { get; set; } = new List<OlcuBirimi>();

    public virtual ICollection<OlcuBirimi> OlcuBirimiOlusturanKullaniciNavigations { get; set; } = new List<OlcuBirimi>();

    public virtual Kullanici? OlusturanKullaniciNavigation { get; set; }

    public virtual ICollection<Sorumlu> SorumluGuncelleyenKullaniciNavigations { get; set; } = new List<Sorumlu>();

    public virtual ICollection<Sorumlu> SorumluOlusturanKullaniciNavigations { get; set; } = new List<Sorumlu>();

    public virtual ICollection<StokDurum> StokDurumGuncelleyenKullaniciNavigations { get; set; } = new List<StokDurum>();

    public virtual ICollection<StokDurum> StokDurumOlusturanKullaniciNavigations { get; set; } = new List<StokDurum>();

    public virtual ICollection<Stok> StokGuncelleyenKullaniciNavigations { get; set; } = new List<Stok>();

    public virtual ICollection<StokHareket> StokHareketGuncelleyenKullaniciNavigations { get; set; } = new List<StokHareket>();

    public virtual ICollection<StokHareket> StokHareketOlusturanKullaniciNavigations { get; set; } = new List<StokHareket>();

    public virtual ICollection<Stok> StokOlusturanKullaniciNavigations { get; set; } = new List<Stok>();
}