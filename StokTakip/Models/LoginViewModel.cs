using System.ComponentModel.DataAnnotations;

namespace StokTakip.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
    [Display(Name = "Kullanıcı Adı")]
    public string KulUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string KulSifre { get; set; } = string.Empty;
}