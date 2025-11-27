using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using StokTakip.Services;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace StokTakip.Controllers // Namespace'i kontrol edin
{
    [Authorize] // Bu özniteliði ekleyin
    public class HomeController : Controller
    {
        private readonly StokTakipBgcContext _context;
        private readonly IUyariService _uyariService;

        public HomeController(StokTakipBgcContext context, IUyariService uyariService)
        {
            _context = context;
            _uyariService = uyariService;
        }

        public async Task<IActionResult> Index()
        {
            // Toplam stok miktarını hesapla (tüm stok durumlarındaki toplam miktar)
            var toplamStokMiktari = await _context.StokDurums
                .Where(sd => sd.DurumMiktar > 0)
                .SumAsync(sd => sd.DurumMiktar);

            // Uyarıları getir
            var uyarilar = await _uyariService.GetUyarilarAsync();
            var okunmamisSayisi = await _uyariService.GetUnreadCountAsync();

            var viewModel = new HomeViewModel
            {
                // Kartlar için verileri çekiyoruz
                ToplamStokKartiSayisi = await _context.Stoks.CountAsync(),
                ToplamAnaDepoSayisi = await _context.Depos.CountAsync(),
                ToplamKullaniciSayisi = await _context.Kullanicis.CountAsync(),
                ToplamStokAdedi = toplamStokMiktari,

                // "Son 10 Stok Hareketi" tablosu için verileri çekiyoruz
                // Ekran görüntüsündeki gibi tarihe göre en yeni olanlarý getirmesi için sýralamayý güncelledik.
                SonStokHareketleri = await _context.StokHarekets
                                                   .OrderByDescending(h => h.HareketTarihi)
                                                   .Take(10)
                                                   .Include(h => h.Stok)
                                                   .Include(h => h.DepoEslestirme)
                                                       .ThenInclude(de => de.Depo)
                                                   .ToListAsync(),

                // Uyarıları ekle
                Uyarilar = uyarilar,
                OkunmamisUyariSayisi = okunmamisSayisi
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MarkUyariAsRead(int uyariId)
        {
            await _uyariService.MarkAsReadAsync(uyariId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllUyarilarAsRead()
        {
            await _uyariService.MarkAllAsReadAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddUyari(string baslik, string mesaj, string tip, string aksiyonUrl = null, string aksiyonText = null)
        {
            var uyariTipi = Enum.Parse<UyariTipi>(tip);
            var uyari = new UyariModel
            {
                Baslik = baslik,
                Mesaj = mesaj,
                Tip = uyariTipi,
                AksiyonUrl = aksiyonUrl,
                AksiyonText = aksiyonText
            };

            await _uyariService.AddUyariAsync(uyari);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetUyarilarAsync()
        {
            var uyarilar = await _uyariService.GetUyarilarAsync();
            Console.WriteLine($"🔍 GetUyarilarAsync çağrıldı, {uyarilar.Count} uyarı döndürülüyor");
            return Json(new { success = true, uyarilar = uyarilar });
        }

        [HttpPost]
        public async Task<IActionResult> SilUyari(int uyariId)
        {
            try
            {
                Console.WriteLine($"🗑️ SilUyari çağrıldı: UyariId={uyariId}");
                await _uyariService.SilUyariAsync(uyariId);
                Console.WriteLine($"✅ Uyarı başarıyla silindi: {uyariId}");
                return Json(new { success = true, message = "Uyarı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Uyarı silinirken hata: {ex.Message}");
                return Json(new { success = false, message = "Uyarı silinirken hata oluştu: " + ex.Message });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            try
            {
                var errorViewModel = new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
                };
                
                // Exception bilgisini al
                var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                if (exceptionFeature != null)
                {
                    var exception = exceptionFeature.Error;
                    errorViewModel.ErrorMessage = exception.Message;
                    
                    // Development ortamında detaylı hata bilgisi
                    var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                    if (environment?.IsDevelopment() == true)
                    {
                        errorViewModel.ErrorDetails = exception.ToString();
                    }
                }
                
                return View(errorViewModel);
            }
            catch
            {
                // Error action'ında hata olursa basit bir hata sayfası döndür
                return View(new ErrorViewModel 
                { 
                    RequestId = "Unknown",
                    ErrorMessage = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin."
                });
            }
        }
    }
}