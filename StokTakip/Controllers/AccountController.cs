using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System.Collections.Generic;
using System.Security.Claims; // Bu using ifadesinin olduğundan emin olun
using System.Threading.Tasks;

namespace StokTakip.Controllers
{
    public class AccountController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public AccountController(StokTakipBgcContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Kullanicis
                                         .Include(u => u.KulTipNavigation)
                                         .FirstOrDefaultAsync(u => u.KulUsername == model.KulUsername && u.KulSifre == model.KulSifre);

                if (user != null && user.KulTipNavigation != null)
                {
                    // Pasif kullanıcı kontrolü
                    if (!user.Statu)
                    {
                        ModelState.AddModelError(string.Empty, "Hesabınız pasif durumda. Lütfen sistem yöneticisi ile iletişime geçin.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.KulUsername),
                        new Claim("FullName", $"{user.KulAd} {user.KulSoyad}"),
                        new Claim(ClaimTypes.Role, user.KulTipNavigation.KultipAdi)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties { };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    TempData["JustLoggedIn"] = true;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}