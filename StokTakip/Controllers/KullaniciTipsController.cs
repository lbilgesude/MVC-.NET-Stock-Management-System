using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StokTakip.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KullaniciTipsController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public KullaniciTipsController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var kullaniciTipleri = _context.KullaniciTips.AsQueryable();

            if (showOnlyActive)
            {
                kullaniciTipleri = kullaniciTipleri.Where(k => k.Statu);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                kullaniciTipleri = kullaniciTipleri.Where(k => k.KultipAdi.Contains(searchString));
            }
            return View(await kullaniciTipleri.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var kullaniciTip = await _context.KullaniciTips
                .Include(k => k.OlusturanKullaniciNavigation)
                .Include(k => k.GuncelleyenKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (kullaniciTip == null) return NotFound();
            return View(kullaniciTip);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KultipAdi,Statu")] KullaniciTip kullaniciTip)
        {
            if (ModelState.IsValid)
            {
                kullaniciTip.OlusturmaTarihi = DateTime.Now;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    kullaniciTip.OlusturanKullanici = int.Parse(userIdString);
                }

                _context.Add(kullaniciTip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(kullaniciTip);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var kullaniciTip = await _context.KullaniciTips.FindAsync(id);
            if (kullaniciTip == null) return NotFound();
            return View(kullaniciTip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KultipAdi,Statu")] KullaniciTip kullaniciTip)
        {
            if (id != kullaniciTip.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var kullaniciTipFromDb = await _context.KullaniciTips.FindAsync(id);
                    if (kullaniciTipFromDb == null) return NotFound();

                    kullaniciTipFromDb.KultipAdi = kullaniciTip.KultipAdi;
                    kullaniciTipFromDb.Statu = kullaniciTip.Statu;
                    kullaniciTipFromDb.GuncellemeTarihi = DateTime.Now;
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        kullaniciTipFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KullaniciTipExists(kullaniciTip.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(kullaniciTip);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var kullaniciTip = await _context.KullaniciTips.FirstOrDefaultAsync(m => m.Id == id);
            if (kullaniciTip == null) return NotFound();
            return View(kullaniciTip);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kullaniciTip = await _context.KullaniciTips.FindAsync(id);
            if (kullaniciTip != null)
            {
                // Önce bu kullanıcı tipini kullanan tüm kullanıcıları sil
                var kullanicilar = await _context.Kullanicis
                    .Where(k => k.KulTip == id)
                    .ToListAsync();
                
                foreach (var kullanici in kullanicilar)
                {
                    // Kullanıcı ile ilgili tüm stok hareketlerini sil
                    var hareketler = await _context.StokHarekets
                        .Where(sh => sh.OlusturanKullanici == kullanici.Id || sh.GuncelleyenKullanici == kullanici.Id)
                        .ToListAsync();
                    _context.StokHarekets.RemoveRange(hareketler);
                    
                    // Kullanıcı ile ilgili tüm stok durumlarını sil
                    var stokDurumlari = await _context.StokDurums
                        .Where(sd => sd.OlusturanKullanici == kullanici.Id || sd.GuncelleyenKullanici == kullanici.Id)
                        .ToListAsync();
                    _context.StokDurums.RemoveRange(stokDurumlari);
                    
                    // Kullanıcı ile ilgili tüm stokları sil
                    var stoklar = await _context.Stoks
                        .Where(s => s.OlusturanKullanici == kullanici.Id || s.GuncelleyenKullanici == kullanici.Id)
                        .ToListAsync();
                    
                    foreach (var stok in stoklar)
                    {
                        // Stok ile ilgili tüm stok hareketlerini sil
                        var stokHareketleri = await _context.StokHarekets
                            .Where(sh => sh.StokId == stok.Id)
                            .ToListAsync();
                        _context.StokHarekets.RemoveRange(stokHareketleri);
                        
                        // Stok durumlarını sil
                        var stokDurumlar = await _context.StokDurums
                            .Where(sd => sd.StokId == stok.Id)
                            .ToListAsync();
                        _context.StokDurums.RemoveRange(stokDurumlar);
                        
                        // Stoku sil
                        _context.Stoks.Remove(stok);
                    }
                    
                    // Kullanıcıyı sil
                    _context.Kullanicis.Remove(kullanici);
                }
                
                // Son olarak kullanıcı tipini sil
                _context.KullaniciTips.Remove(kullaniciTip);
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var kullaniciTipleriQuery = _context.KullaniciTips.AsQueryable(); // Tüm kullanıcı tiplerini export et (aktif ve pasif)
            if (!String.IsNullOrEmpty(searchString))
            {
                kullaniciTipleriQuery = kullaniciTipleriQuery.Where(k => k.KultipAdi.Contains(searchString));
            }
            var data = await kullaniciTipleriQuery.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Kullanıcı Tipleri");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Kullanıcı Tip Adı";
                worksheet.Cell(currentRow, 2).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var item in data)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.KultipAdi;
                    worksheet.Cell(currentRow, 2).Value = item.Statu ? "Aktif" : "Pasif";
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"KullaniciTipleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool KullaniciTipExists(int id)
        {
            return _context.KullaniciTips.Any(e => e.Id == id);
        }
    }
}