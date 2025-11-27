using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;

namespace StokTakip.Controllers
{
    public class TestController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public TestController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new
            {
                ToplamHareket = await _context.StokHarekets.CountAsync(),
                ToplamStok = await _context.Stoks.CountAsync(),
                ToplamDepo = await _context.Depos.CountAsync(),
                ToplamAltDepo = await _context.AltDepos.CountAsync(),
                ToplamStokDurum = await _context.StokDurums.CountAsync(),
                ToplamHareketTip = await _context.HareketTips.CountAsync(),
                ToplamKullanici = await _context.Kullanicis.CountAsync(),
                
                // Son 5 hareket
                SonHareketler = await _context.StokHarekets
                    .Include(h => h.Stok)
                    .Include(h => h.HareketTipNavigation)
                    .OrderByDescending(h => h.HareketTarihi)
                    .Take(5)
                    .Select(h => new
                    {
                        h.HareketTarihi,
                        StokAd = h.Stok.StokAd,
                        HareketTip = h.HareketTipNavigation.HareketTipAdi,
                        h.HareketMiktar
                    })
                    .ToListAsync(),
                
                // Hareket tipleri
                HareketTipleri = await _context.HareketTips
                    .Select(ht => new
                    {
                        ht.HareketTipAdi,
                        ht.IslemGostergesi
                    })
                    .ToListAsync(),
                
                // Stok durumları
                StokDurumlari = await _context.StokDurums
                    .Include(sd => sd.Stok)
                    .Include(sd => sd.DepoEslestirme)
                        .ThenInclude(de => de.Depo)
                    .Take(5)
                    .Select(sd => new
                    {
                        StokAd = sd.Stok.StokAd,
                        DepoAd = sd.DepoEslestirme.Depo.DepoAdi,
                        sd.DurumMiktar
                    })
                    .ToListAsync()
            };

            return View(model);
        }
    }
}
